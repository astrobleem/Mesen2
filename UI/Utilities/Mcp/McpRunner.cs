using Mesen.Config;
using Mesen.Debugger.Utilities;
using Mesen.Interop;
using System;
using System.IO;

namespace Mesen.Utilities.Mcp;

/// <summary>
/// Entry for --mcp mode. Mirrors TestRunner.Run for ROM load / emulator
/// bring-up, then hands control to McpServer which blocks on stdin until
/// the client disconnects.
/// </summary>
internal static class McpRunner
{
	private const int DefaultPort = 7333;

	/// <summary>Parse an explicit --mcp-port=N flag; default otherwise.</summary>
	private static int ParsePort(string[] args)
	{
		foreach(string arg in args) {
			string a = arg.StartsWith("--") ? arg.Substring(2) : arg.StartsWith("-") || arg.StartsWith("/") ? arg.Substring(1) : arg;
			if(a.StartsWith("mcp-port=", StringComparison.OrdinalIgnoreCase)) {
				string v = a.Substring("mcp-port=".Length);
				if(int.TryParse(v, out int port) && port > 0 && port < 65536) {
					return port;
				}
			}
		}
		return DefaultPort;
	}

	internal static int Run(string[] args)
	{
		Log("run entered");
		ConfigManager.DisableSaveSettings = true;
		CommandLineHelper cli = new(args, true);

		if(cli.FilesToLoad.Count != 1) {
			Log($"expected one ROM, got {cli.FilesToLoad.Count}");
			return -1;
		}

		Log("InitDll");
		EmuApi.InitDll();
		Log("ApplyConfig");
		ConfigManager.Config.ApplyConfig();
		Log("InitializeEmu");
		// Enable audio (noAudio=false) so the SPC700/DSP run and record_audio
		// can capture a real waveform. We still pass noVideo=true (we don't
		// open an Avalonia window), and noInput=true (debugger overrides
		// only). Audio is emulated but not pushed to the speakers in
		// console mode unless the user explicitly wants it.
		EmuApi.InitializeEmu(ConfigManager.HomeFolder, IntPtr.Zero, IntPtr.Zero, true, false, true, true);
		Log("Pause");
		EmuApi.Pause();

		ConfigApi.SetEmulationFlag(EmulationFlags.ConsoleMode, true);
		// Tell the C++ side that stdout belongs to MCP; Debugger/Lua logs
		// divert to stderr.
		ConfigApi.SetEmulationFlag(EmulationFlags.McpMode, true);

		Log("LoadRom");
		if(!EmuApi.LoadRom(cli.FilesToLoad[0], string.Empty)) {
			Log("failed to load ROM");
			return -1;
		}

		// Re-pause immediately after load instead of auto-resuming: LoadRom's
		// native implementation resumes the core internally regardless of the
		// pre-load Pause() above. This MUST happen before
		// DebugWorkspaceManager.Load()/Lua loading below, not after: those
		// calls do real disk I/O (symbol files, workspace state) and can take
		// several hundred milliseconds of wall-clock time. Until MaximumSpeed
		// is set (further down), the emulator runs at normal, unaccelerated
		// speed, so that wall-clock delay was previously translating directly
		// into dozens of real frames elapsing -- nondeterministically, since
		// it depended on disk/OS scheduling timing -- before any client could
		// ever call pause(). That made frame 0 unobservable and produced
		// exactly the kind of run-to-run-varying "crash point" that looks
		// like nondeterministic game behavior but is actually harness skew.
		// The client now owns the first resume() call and gets a true
		// from-reset-vector trace.
		Log("Pause (post-load)");
		EmuApi.Pause();

		Log("DebugWorkspaceManager.Load");
		DebugWorkspaceManager.Load();

		// Load any Lua scripts specified on the command line — useful for
		// seeding state before the MCP session takes over.
		foreach(string luaScript in cli.LuaScriptsToLoad) {
			try {
				string script = File.ReadAllText(luaScript);
				DebugApi.LoadScript(luaScript, Path.GetDirectoryName(luaScript) ?? Program.OriginalFolder, script);
			} catch(Exception ex) {
				Console.Error.WriteLine($"mcp: failed to load Lua {luaScript}: {ex.Message}");
			}
		}

		ConfigApi.SetEmulationFlag(EmulationFlags.MaximumSpeed, true);

		// The core may have serviced a partial frame between LoadRom()'s
		// internal resume and the Pause() above taking effect (Pause() only
		// halts at the next frame boundary). Re-assert pause once more here,
		// after workspace/Lua loading, so any such stray progress is also
		// caught before the client connects.
		Log("Pause (post-workspace-load)");
		EmuApi.Pause();

		// Hand control to the MCP server. Blocks until client disconnects
		// and accept() unblocks (we rely on emu.Stop from a tool for shutdown).
		int port = ParsePort(args);
		Log($"McpServer on port {port}");
		McpServer server = new(port);
		server.Run();
		Log("McpServer returned");

		EmuApi.Stop();
		EmuApi.Release();
		return 0;
	}

	// Diagnostic logging — writes to a file so WinExe stdio quirks don't hide
	// the trace. Also mirrors to stderr for console-launched usage.
	private static string LogPath = System.IO.Path.Combine(
		Config.ConfigManager.HomeFolder, "mcp_runner.log"
	);

	private static void Log(string message)
	{
		string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
		try {
			System.IO.File.AppendAllText(LogPath, line + Environment.NewLine);
		} catch { }
		try {
			Console.Error.WriteLine(line);
		} catch { }
	}
}
