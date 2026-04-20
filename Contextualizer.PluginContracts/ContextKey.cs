using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.PluginContracts
{
    public class ContextKey
    {
        public static string _self = "_self";
        public static string _body = "_body";
        public static string _formatted_output = "_formatted_output";
        public static string _input = "_input";
        public static string _count = "_count";
        public static string _selector_key = "_selector_key";
        public static string _notification_title = "_notification_title";
        public static string _error = "_error";
        public static string _match = "_match";
        public static string _duration = "_duration";
        public static string _trigger = "_trigger";
        public static string _shell_working_directory = "_shell_working_directory";
        public static string _shell_timeout_seconds = "_shell_timeout_seconds";
        public static string _shell_stdout = "_shell_stdout";
        public static string _shell_stderr = "_shell_stderr";
        public static string _shell_exit_code = "_shell_exit_code";
        public static string _shell_timed_out = "_shell_timed_out";
        public static string _shell_elapsed_ms = "_shell_elapsed_ms";
        public static string _shell_stdout_key = "_shell_stdout_key";
        public static string _shell_stderr_key = "_shell_stderr_key";
        public static string _shell_exit_code_key = "_shell_exit_code_key";
        public static string _shell_timed_out_key = "_shell_timed_out_key";
        public static string _shell_elapsed_ms_key = "_shell_elapsed_ms_key";
    }
}
