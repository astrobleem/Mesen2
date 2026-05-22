using System;
using Avalonia;
using ReactiveUI.SourceGenerators;

namespace Nexen.Config; 
public sealed partial class MainWindowConfig : BaseWindowConfig<MainWindowConfig> {
	public MainWindowConfig() {
		WindowSize = new PixelSize(1280, 820);
		WindowLocation = new PixelPoint(40, 40);
		WindowIsMaximized = true;
	}
}
