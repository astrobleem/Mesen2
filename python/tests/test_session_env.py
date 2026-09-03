import os
import unittest
from unittest.mock import patch

from mesen_mcp.session import McpSession


class SessionEnvironmentTests(unittest.TestCase):
    def test_explicit_environment_is_passed_without_mutating_parent(self):
        child_env = {
            "MESEN_TEST_CHILD": "capture-path",
            "MESEN_TEST_SECOND": "voice-6",
        }
        session = McpSession("game.sfc", "Mesen.exe", env=child_env)

        self.assertEqual(session._env["MESEN_TEST_CHILD"], "capture-path")
        self.assertEqual(session._env["MESEN_TEST_SECOND"], "voice-6")
        self.assertNotIn("MESEN_TEST_CHILD", os.environ)

        with patch("mesen_mcp.session.validate_mesen_build"), patch(
            "mesen_mcp.session.subprocess.Popen", return_value=object()
        ) as popen, patch.object(session, "_connect", return_value=object()), patch.object(
            session, "call"
        ):
            session.__enter__()
            self.assertIs(popen.call_args.kwargs["env"], session._env)


if __name__ == "__main__":
    unittest.main()
