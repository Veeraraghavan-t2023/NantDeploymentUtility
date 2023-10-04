///Revision History
///----------------------------------------------------------
///Date         Author                 Description
///12 Jun 2023  Veeraraghavan          Creation.
///

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;

namespace NantDeploymentUtility
{
    /// <summary>
    /// This class contains logic for selecting the configuration for Nant build, running the command and copying to shared drive.
    /// </summary>
    class UtilityViewModel : INotifyPropertyChanged
    {
        #region Constants
        private const string NANT_PATH = @"C:\nant-0.86\bin";
        private const string DEPLOY_CONFIG = "gateway.deploy.local.build";
        private const string ORIGINAL_BRANCH = "original.branch";
        private const string CODE_PATH = @"D:\Builds\StratosGateway\source";
        private const string SLASH = @"\";
        private const string DOT_NET_4_BUILD = @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe";
        private const string DOT_NET_4_8_BUILD = @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe";
        private const string DOT_NET_4_8_VS_2022_BUILD = @"C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe";
        private const string MS_BUILD = "msbuild";
        private const string DOT_NET_4_8 = ".Net 4.8";
        private const string DOT_NET_4_0 = ".Net 4.0";
        private const string DEV_ENVIRONMENT = "Dev";
        private const string TEST_ENVIRONMENT = "Test";
        private const string STAGE_ENVIRONMENT = "Stage";
        private const string PROPERTY_ELEMENT = "property";
        private const string PROJECT_ELEMENT = "project";
        private const string ERROR_NANT_NOT_FOUND = "Nant command utility is not available in the system!";
        private const string ERROR_BUILD_FOLDER_NOT_FOUND = "The build folder given is incorrect";
        private const string ERROR_DOT_NET_4_8_NOT_FOUND = "Dot Net 4.8 build is not found, please install .Net 4.8";
        private const string ERROR_DOT_NET_4_0_NOT_FOUND = "Dot Net 4.0 build is not found, please install .Net 4.0";
        private const string ENV1 = "ENV1";
        private const string ENV2 = "ENV2";
        private const string ENV3 = "ENV3";
        private const string ENV4 = "ENV4";
        private const string ENV5 = "ENV5";
        private const string MAIN = "Main";
        private const string NANT_EXE = @"C:\nant-0.86\bin\Nant.exe";
        private const string COMMAND = "-buildfile:gateway.deploy.local.build Deploy -D:Build.Configuration={0} -D:Application.Environment={1} -D:Destination.ServerName={2} -D:Dist.Only=false";
        private const string STAGE_SERVER = "CAMPWEBSTG1";
        private const string DEV_SERVER = "CAMPWEBDEV1";
        private const string TEST_SERVER = "CAMPWEBTST1";
        private const string DEPLOY_PATH = @"D:\Builds\deploy\StratosGateway\{0}\{1}\dist\";
        private const string NETWORK_SHARE_PATH = @"\\gemini.inmarsat.local\month\L3_Support\";
        private const string FILE_COPIED_SUCCESS_MESSAGE = "{0} files copied successfully. Time taken: {1}";
        private const string ZIP_FILE_COPIED = "{0} zip file copied successfully. Time taken: {1}";
        private const string ERROR_BUILD_NOT_GENERATED = "Build is not generated for the selected configuration!";
        private const string UNDER_SCORE = "_";
        private const string ZIP_EXTENSION = "zip";
        private const string DOT = ".";
        private const string CONFIG = "config";
        private const string EXE_CONFIG = "exe.config";
        private const string TIME_FORMAT_DETAIL = "HH_mm_ss";
        private const string TIME_FORMAT = "h'h 'm'm 's's'";
        private const string ZIP_DEPLOY_PATH = @"D:\Builds\deploy\";
        private const string ERROR_NANT_EXECUTION = "Error occurred during Nant build command execution:  ";
        private const string ERROR_COPYING = "Error occurred during Copying files to shared path: ";
        private const string COPYING_IN_PROGRESS = "Copying is in Progress....";
        private const string LATEST_TOOL_PATH = @"\\gemini.inmarsat.local\month\L3_Support\Nant Deployment Utility Tool\";
        private const string TOOL_NAME = "NantDeploymentUtility.exe";//"NantDeploymentUtility.exe";
        private const string TOOL_VERSION_INFO = "You are using the latest tool version: {0}";
        private const string BACK_UP_EXTENSION = ".bak";
        #endregion

        #region Private Variables
        private string _buildFolder;
        private string _targetFramework;
        private string _environment;
        private string _serverName;
        private List<string> _servers;
        private List<string> _targetFrameworks;
        private List<string> _buildPaths;
        private List<string> _environments;
        private bool _enableRun = false;
        private bool _includeConfigFiles = false;
        private string _errorMessage = string.Empty;
        private XDocument _deploymentDocument;
        private readonly string CONFIG_FILE_FULL_PATH;
        private readonly string ERROR_DEPLOY_CONFIG_NOT_FOUND;       
        private ICommand _saveCommand;
        private ICommand _runCommand;       
        private ICommand _closeCommand;
        private ICommand _aboutCommand;
        private ICommand _copyCommand;
        private string _outputMessage;
        private string _destination;      
        private const string FILES_COPIED = "Files Copied: {0}, Time elapsed: {1}";
        private string _filesCopied = "";
        private Visibility _enableFileDetails = Visibility.Hidden;
        private bool _enableCopyCommand = true;
        private Stopwatch _timer;
        private int _sourceFileCount;
        private bool _isVS2019 = true;
        private bool _isVS2022 = false;
        private bool _hideVisualStudioVersionSelection = false;
        #endregion

        #region Properties
        /// <summary>
        /// This property is used to display the output messages in console window during Nant command execution.
        /// </summary>
        public string OutputMessage { get { return _outputMessage; } set { _outputMessage = value; OnPropertyRaised("OutputMessage"); } }
        /// <summary>
        /// The build folder
        /// </summary>
        public string BuildFolder { get { return _buildFolder; } set { _buildFolder = value; OnPropertyRaised("BuildFolder"); } }
        /// <summary>
        /// Target .Net Framework
        /// </summary>
        public string TargetFramework
        {
            get { return _targetFramework; }
            set
            {
                _targetFramework = value;
                OnPropertyRaised("TargetFramework");
                if (value == DOT_NET_4_0) { HideVisualStudioVersionSelection = false; }
                else
                {
                    HideVisualStudioVersionSelection = true;
                }
            }
        }
        /// <summary>
        /// Environment name
        /// </summary>
        public string EnvironmentName { get { return _environment; } set { _environment = value; DestinationFolder = value; GetSourceFilePathCount = value; OnPropertyRaised("Environment"); } }
        /// <summary>
        /// Server Name like Stage, Test, Dev.
        /// </summary>
        public string ServerName { get { return _serverName; } set { _serverName = value; DestinationFolder = value; GetSourceFilePathCount = value; OnPropertyRaised("ServerName"); } }
        public List<string> Servers { get { return _servers; } set { _servers = value; OnPropertyRaised("Servers"); } }
        public List<string> TargetFrameworks { get { return _targetFrameworks; } set { _targetFrameworks = value; OnPropertyRaised("TargetFrameworks"); } }
        public List<string> BuildPaths { get { return _buildPaths; } set { _buildPaths = value; OnPropertyRaised("BuildPaths"); } }
        public List<string> Environments { get { return _environments; } set { _environments = value; OnPropertyRaised("Environments"); } }
        public string ErrorMessage { get { return _errorMessage; } set { _errorMessage = value; OnPropertyRaised("ErrorMessage"); } }
        public bool EnableRunCommand { get { return _enableRun; } set { _enableRun = value; OnPropertyRaised("EnableRunCommand"); } }
        public bool IsVS2019 { get { return _isVS2019; } set { _isVS2019 = value; OnPropertyRaised("IsVS2019"); } }
        public bool IsVS2022 { get { return _isVS2022; } set { _isVS2022 = value; OnPropertyRaised("IsVS2022"); } }
        public bool HideVisualStudioVersionSelection { get { return _hideVisualStudioVersionSelection; } set { _hideVisualStudioVersionSelection = value; OnPropertyRaised("HideVisualStudioVersionSelection"); } }
        public string FilesCopied
        {
            get { return _filesCopied; }
            set { _filesCopied = value; OnPropertyRaised("FilesCopied"); }
        }
        public Visibility EnableFileDetails
        {
            get { return _enableFileDetails; }
            set { _enableFileDetails = value; OnPropertyRaised("EnableFileDetails"); }
        }
        public string DestinationFolder
        {
            get { _destination = NETWORK_SHARE_PATH + EnvironmentName + "_" + ServerName + "_" + DateTime.Today.ToString("dd-MMM-yyyy"); return _destination; }
            set { _destination = NETWORK_SHARE_PATH + EnvironmentName + "_" + ServerName + "_" + DateTime.Today.ToString("dd-MMM-yyyy"); OnPropertyRaised("DestinationFolder"); }
        }

        public bool EnableCopyCommand { get { return _enableCopyCommand; } set { _enableCopyCommand = value; OnPropertyRaised("EnableCopyCommand"); } }

        public bool CanExecute
        {
            get
            {
                // check if executing is allowed, i.e., validate, check if a process is running, etc. 
                return true;
            }
        }

        private IEnumerable<XAttribute> GetAttributesFromConfigXml
        {
            get
            {
                _deploymentDocument = XDocument.Load(CONFIG_FILE_FULL_PATH);
                var buildPath = (from x in _deploymentDocument.Elements(PROJECT_ELEMENT)
                                 select x.Elements(PROPERTY_ELEMENT).Attributes()).FirstOrDefault();
                return buildPath;
            }
        }

        private string GetSourceFilePathCount
        {
            set
            {
                var sourceFilePath = string.Format(DEPLOY_PATH, ServerName, EnvironmentName);
                if (Directory.Exists(sourceFilePath))
                {
                    // searches the current directory and sub directory
                    _sourceFileCount = Directory.GetFiles(sourceFilePath, "*", SearchOption.AllDirectories).Length;
                    EnableCopyCommand = true;
                }
                else
                {
                    EnableCopyCommand = false;
                }
            }
        }

        public bool IncludeConfigFiles
        {
            get { return _includeConfigFiles; }
            set { _includeConfigFiles = value; OnPropertyRaised("IncludeConfigFiles"); }
        }

        #endregion

        #region Commands
        /// <summary>
        /// Copy Files command
        /// </summary>
        public ICommand CopyCommand
        {
            get
            {
                return _copyCommand ?? (_copyCommand = new CommandHandler(() => CopyToDestination(), () => CanExecute));
            }
        }

        /// <summary>
        /// Save command
        /// </summary>
        public ICommand SaveCommand
        {
            get
            {
                return _saveCommand ?? (_saveCommand = new CommandHandler(() => SaveAction(), () => CanExecute));
            }
        }

        /// <summary>
        /// Run Command
        /// </summary>
        public ICommand RunCommand
        {
            get
            {
                return _runCommand ?? (_runCommand = new CommandHandler(() => RunProcess(), () => CanExecute));
            }
        }
        
        /// <summary>
        /// Close Command
        /// </summary>
        public ICommand CloseCommand
        {
            get
            {
                return _closeCommand ?? (_closeCommand = new CommandHandler(() => CloseAction(), () => CanExecute));
            }
        }

        public ICommand AboutTool
        {
            get
            {
                return _aboutCommand ?? (_aboutCommand = new CommandHandler(() => AboutToolAction(), () => CanExecute));
            }
        }

        private void AboutToolAction()
        {
            try
            {
               
                const string TOOL_TITLE = "Nant Deployment Utility";
                Assembly currentAssembly = Assembly.GetExecutingAssembly();
                FileVersionInfo fileVersion = FileVersionInfo.GetVersionInfo(currentAssembly.Location);
                Assembly latestAssembly = Assembly.LoadFile(LATEST_TOOL_PATH + TOOL_NAME);
                FileVersionInfo latestFileVersion = FileVersionInfo.GetVersionInfo(latestAssembly.Location);
                if (string.Compare(latestFileVersion.FileVersion, fileVersion.FileVersion) == 0)
                {
                    MessageBox.Show(string.Format(TOOL_VERSION_INFO, fileVersion.FileVersion),
                       TOOL_TITLE, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    var messageResult = MessageBox.Show("You are using old version of the Tool, do you want to update?",
                        TOOL_TITLE, MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
                    if (messageResult.Equals(MessageBoxResult.Yes))
                    {
                        MessageBox.Show("Application is restarting!...");
                        Application.Current.Shutdown();
                        string backupFile = currentAssembly.Location + BACK_UP_EXTENSION;
                        if (File.Exists(backupFile))
                        {
                            File.Delete(backupFile);
                        }
                        File.Move(currentAssembly.Location, backupFile);
                        File.Copy(LATEST_TOOL_PATH + TOOL_NAME, currentAssembly.Location, true);
                        System.Windows.Forms.Application.Restart();
                    }
                }
            }
            catch
            {
                MessageBox.Show("Error occurred while checking for updates!", TOOL_NAME, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
               
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Initialize members
        /// </summary>
        public UtilityViewModel()
        {
            CONFIG_FILE_FULL_PATH = NANT_PATH + SLASH + DEPLOY_CONFIG;
            ERROR_DEPLOY_CONFIG_NOT_FOUND = "Deploy config " + DEPLOY_CONFIG + " does not exist or is in incorrect format!";
            UpdateComboBoxItems();
            ReadCurrentBuildFolder();
            ReadTargetFramework();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Reads the current build folder
        /// </summary>
        private void ReadCurrentBuildFolder()
        {
            if (!Directory.Exists(NANT_PATH))
            {
                ErrorMessage = ERROR_NANT_NOT_FOUND;
                return;
            }
            var buildPath = GetAttributesFromConfigXml;

            if (buildPath == null)
            {
                ErrorMessage = ERROR_DEPLOY_CONFIG_NOT_FOUND;
                return;
            }
            BuildFolder = buildPath.SingleOrDefault(x => x.Value == ORIGINAL_BRANCH).NextAttribute.Value;
        }

        /// <summary>
        /// Reads the Target Framework
        /// </summary>
        private void ReadTargetFramework()
        {
            var buildPath = GetAttributesFromConfigXml;
            var path = buildPath.SingleOrDefault(x => x.Value == MS_BUILD).NextAttribute.Value;
            if (path == DOT_NET_4_8_BUILD ||
               path == DOT_NET_4_8_VS_2022_BUILD)
            {
                TargetFramework = DOT_NET_4_8;
                HideVisualStudioVersionSelection = true;
            }
            else
            {
                TargetFramework = DOT_NET_4_0;
                HideVisualStudioVersionSelection = false;
            }
        }

        /// <summary>
        /// Set the build folder.
        /// </summary>
        private void SetBuildFolder()
        {
            var buildPath = GetAttributesFromConfigXml;
            if (!Directory.Exists(CODE_PATH + SLASH + BuildFolder))
            {
                ErrorMessage = ERROR_BUILD_FOLDER_NOT_FOUND;
                return;
            }
            buildPath.SingleOrDefault(x => x.Value == ORIGINAL_BRANCH).NextAttribute.Value = BuildFolder;
            _deploymentDocument.Save(CONFIG_FILE_FULL_PATH);
        }

        /// <summary>
        /// Sets the Target framework
        /// </summary>
        private void SetTargetFramework()
        {
            var buildPath = GetAttributesFromConfigXml;
            ErrorMessage = string.Empty;
            EnableRunCommand = true;
            switch (TargetFramework)
            {
                case DOT_NET_4_8:
                    {
                        if (IsVS2019)
                        {
                            if (!File.Exists(DOT_NET_4_8_BUILD))
                            {
                                ErrorMessage = ERROR_DOT_NET_4_8_NOT_FOUND;
                                EnableRunCommand = false;
                                return;
                            }
                            buildPath.SingleOrDefault(x => x.Value == MS_BUILD).NextAttribute.Value = DOT_NET_4_8_BUILD;
                        }
                        else if (IsVS2022)
                        {
                            if (!File.Exists(DOT_NET_4_8_VS_2022_BUILD))
                            {
                                ErrorMessage = ERROR_DOT_NET_4_8_NOT_FOUND;
                                EnableRunCommand = false;
                                return;
                            }
                            buildPath.SingleOrDefault(x => x.Value == MS_BUILD).NextAttribute.Value = DOT_NET_4_8_VS_2022_BUILD;
                        }
                    }
                    break;               
                case DOT_NET_4_0:
                    {
                        if (!File.Exists(DOT_NET_4_BUILD))
                        {
                            ErrorMessage = ERROR_DOT_NET_4_0_NOT_FOUND;
                            EnableRunCommand = false;
                            return;
                        }
                        buildPath.SingleOrDefault(x => x.Value == MS_BUILD).NextAttribute.Value = DOT_NET_4_BUILD;
                    }
                    break;
                default:
                    {
                        break;
                    }
            }
            _deploymentDocument.Save(CONFIG_FILE_FULL_PATH);
        }
                            
        /// <summary>
        /// Close the app when no longer needed
        /// </summary>
        private void CloseAction()
        {
            App.Current.MainWindow.Close();
        }

        /// <summary>
        /// Save the configuration before Nant execution
        /// </summary>
        private void SaveAction()
        {           
            SetBuildFolder();
            SetTargetFramework();           
        }

        /// <summary>
        /// Update the dropdown fields
        /// </summary>
        private void UpdateComboBoxItems()
        {
            _servers = new List<string>();
            _servers.Add(ENV1);
            _servers.Add(ENV2);
            _servers.Add(ENV3);
            _servers.Add(ENV4);
            _servers.Add(ENV5);
            _servers.Add(MAIN);
            _targetFrameworks = new List<string>();
            _targetFrameworks.Add(DOT_NET_4_8);
            _targetFrameworks.Add(DOT_NET_4_0);
            _environments = new List<string>();
            _environments.Add(DEV_ENVIRONMENT);
            _environments.Add(TEST_ENVIRONMENT);
            _environments.Add(STAGE_ENVIRONMENT);
            _buildPaths = new List<string>();
            _buildPaths = Directory.GetDirectories(CODE_PATH).ToList().Select(x => x.Replace(CODE_PATH + SLASH, "")).ToList();
        }      

        /// <summary>
        /// Run the nant command in a process asynchronously.
        /// </summary>
        public async void RunProcess()
        {
            try
            {               
                EnableRunCommand = EnableCopyCommand = false;
                OutputMessage = string.Empty;
                FilesCopied = string.Empty;
                string serverToBeDeployed;
                serverToBeDeployed = EnvironmentName == UtilityViewModel.DEV_ENVIRONMENT ?
                    DEV_SERVER : EnvironmentName == UtilityViewModel.STAGE_ENVIRONMENT ?
                    STAGE_SERVER : TEST_SERVER;

                ProcessStartInfo procStartInfo = new ProcessStartInfo();

                procStartInfo.FileName = NANT_EXE;
                procStartInfo.Arguments = string.Format(COMMAND, EnvironmentName.ToUpper(), ServerName, serverToBeDeployed);
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;


                await Task.Run(() =>
                {
                    using (Process process = new Process())
                    {
                        process.StartInfo = procStartInfo;

                        process.EnableRaisingEvents = true;
                        process.OutputDataReceived += OutputDataReceived;
                        process.ErrorDataReceived += ErrorDataReceived;
                        process.Start();
                        process.BeginOutputReadLine();
                        process.WaitForExit();
                    }
                }).ContinueWith(d =>
                            {
                                EnableRunCommand = EnableCopyCommand = true;                               
                            });
            }
            catch(Exception ex)
            {
                ErrorMessage = ERROR_NANT_EXECUTION + ex.Message;
            }            
        }

        /// <summary>
        /// Event that fires when process executes and produces the console output
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            // Process line provided in e.Data
            OutputMessage += e.Data + Environment.NewLine; ;
        }

        /// <summary>
        /// Event that fires when error occurs during process execution
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            //Process line provided in e.Data
            ErrorMessage = e.Data;
        }

        /// <summary>
        /// Copy to destination
        /// </summary>
        private void CopyToDestination()
        {
            try
            {
                EnableCopyCommand = false;
                EnableFileDetails = Visibility.Visible;
                ErrorMessage = string.Empty;
                FilesCopied = string.Empty;

                var sourceFilePath = string.Format(DEPLOY_PATH, ServerName, EnvironmentName);
                var destinationFilePath = DestinationFolder;

                if (!Directory.Exists(DestinationFolder))
                {
                    Directory.CreateDirectory(DestinationFolder);
                }
                if (!Directory.Exists(sourceFilePath))
                {
                    ErrorMessage = ERROR_BUILD_NOT_GENERATED;
                    return;
                }
                //calculate time taken to execute
                _timer = new Stopwatch();
                _timer.Start();
                if (!_includeConfigFiles)
                {
                    RemoveConfigFiles(sourceFilePath);
                }
                CreateZipAndCopy(sourceFilePath);                                
            }
            catch (Exception ex)
            {
                ErrorMessage = ERROR_COPYING;
                MessageBox.Show(ex.Message + ex.StackTrace);
            }
        }

        /// <summary>
        /// Removes the config files before zipping.
        /// </summary>
        /// <param name="sourceFilePath">source file path</param>
        private void RemoveConfigFiles(string sourceFilePath)
        {
            int count = 0;
            var ext = new List<string> { "config", "exe.Config" };
            var myFiles = Directory
                .EnumerateFiles(sourceFilePath, "*.*", SearchOption.AllDirectories)
                .Where(s => ext.Contains(Path.GetExtension(s).TrimStart('.').ToLowerInvariant()));
            foreach(var file in myFiles)
            {
                File.Delete(file);
                count++;
            }
        }

        /// <summary>
        /// Create Zip folder and copy to shared path
        /// </summary>
        /// <param name="sourceFilePath"></param>
        private async void CreateZipAndCopy(string sourceFilePath)
        {
            string zipPath = string.Empty;
            string purpose = "WebApps";
            if (TargetFramework == DOT_NET_4_0)
            {
                purpose = "Executables";
            }

            string fileName = EnvironmentName + UNDER_SCORE +
                              ServerName + UNDER_SCORE +
                              purpose + UNDER_SCORE +
                              DateTime.Now.ToString(TIME_FORMAT_DETAIL) +
                              DOT + ZIP_EXTENSION;
            await Task.Run(() =>
            {

                zipPath = ZIP_DEPLOY_PATH + fileName ;
                if (!Directory.Exists(DestinationFolder))
                {
                    Directory.CreateDirectory(DestinationFolder);
                }
                ZipFile.CreateFromDirectory(sourceFilePath, zipPath);
               
                FileInfo file = new FileInfo(zipPath);
                FilesCopied = COPYING_IN_PROGRESS;
                file.CopyTo(DestinationFolder + SLASH + fileName, true);
               
            }).ContinueWith(d =>
            {
                _timer.Stop();
                TimeSpan timeTaken = _timer.Elapsed;
                FilesCopied = string.Format(ZIP_FILE_COPIED, fileName, timeTaken.ToString(TIME_FORMAT));
                EnableCopyCommand = true;
            });
        }
        
        #endregion

        #region Events
        /// <summary>
        /// On property value set notify the change to UI
        /// </summary>
        /// <param name="propertyname">name of the property</param>
        private void OnPropertyRaised(string propertyname)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }
}
