using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Task = System.Threading.Tasks.Task;

namespace AutoTerminal.Commands
{
    internal sealed class OpenTerminalCommand
    {
        public const int CommandId = 0x0100;

        public static readonly Guid CommandSet = new Guid("3be5cd22-7f9b-4739-8323-87c978d75a66");

        private readonly AsyncPackage package;

        private OpenTerminalCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static OpenTerminalCommand Instance
        {
            get;
            private set;
        }

        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new OpenTerminalCommand(package, commandService);
        }

        private string GetClickedFolderNodePath()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            IntPtr hierarchyPointer, selectionContainerPointer;
            IVsMultiItemSelect multiItemSelect;
            uint projectItemId;

            IVsMonitorSelection monitorSelection =
                    (IVsMonitorSelection)Package.GetGlobalService(
                    typeof(SVsShellMonitorSelection));

            monitorSelection.GetCurrentSelection(out hierarchyPointer,
                                                 out projectItemId,
                                                 out multiItemSelect,
                                                 out selectionContainerPointer);

            IVsHierarchy selectedHierarchy = Marshal.GetTypedObjectForIUnknown(
                                                 hierarchyPointer,
                                                 typeof(IVsHierarchy)) as IVsHierarchy;

            selectedHierarchy.GetCanonicalName(projectItemId, out string folderPath);

            return Path.GetDirectoryName(folderPath).Replace("//", "/");
        }

        private void StartProcess(string directory)
        {
            var process = new System.Diagnostics.Process();
            var startInfo = new ProcessStartInfo
            {
                WorkingDirectory = directory,
                WindowStyle = ProcessWindowStyle.Normal,
                FileName = "cmd.exe",
                RedirectStandardInput = false,
                UseShellExecute = false,
            };

            process.StartInfo = startInfo;
            process.Start();
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var directory = GetClickedFolderNodePath();
            StartProcess(directory);
        }
    }
}
