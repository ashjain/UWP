using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Windows.Foundation.Collections;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.AppExtensions;
using System.ComponentModel;
using Windows.Storage;
using System.Collections.ObjectModel;
using Windows.ApplicationModel;

namespace MyMainExtensibleWin32App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>


    public class Lib1Wrapper
    {
        public static class NativeLibrary
        {
            [DllImport("kernel32.dll")]
            public static extern IntPtr LoadLibrary(string dllToLoad);

            [DllImport("kernel32.dll")]
            public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

            [DllImport("kernel32.dll")]
            public static extern bool FreeLibrary(IntPtr hModule);

        }

        IntPtr _dllhandle = IntPtr.Zero;
        GetVersionDelegate _getstring = null;

        // Delegate with function signature for the GetVersion function 
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U4)]
        delegate UInt32 GetVersionDelegate(
            [OutAttribute][InAttribute] StringBuilder versionString,
            [OutAttribute] UInt32 length);

        public string GetString()
        {
            if (_getstring != null)
            {
                StringBuilder builder = new StringBuilder(1024);
                _getstring(builder, (uint)1024);
                // Return string
                return builder.ToString();
            }

            return "";
        }

        public Lib1Wrapper(string filename)
        {
            _dllhandle = NativeLibrary.LoadLibrary(filename);

            if (_dllhandle == IntPtr.Zero)
            {
                return;
            }

            var get_function_handle = NativeLibrary.GetProcAddress(_dllhandle, "GetString");

            if (get_function_handle != IntPtr.Zero)
            {
                _getstring = (GetVersionDelegate)Marshal.GetDelegateForFunctionPointer(get_function_handle, typeof(GetVersionDelegate));
            }

        }

        ~Lib1Wrapper()
        {
            NativeLibrary.FreeLibrary(_dllhandle);
        }
    }
    public partial class MainWindow : Window
    {
        string appdatafolderpath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void GetDLLBtn_Click(object sender, RoutedEventArgs e)
        {
            ExtensionManager extensionManager = new ExtensionManager("ExtensibleAppDLL");

            String pathToDLLs = await extensionManager.Initialize();

            String DLLPath = pathToDLLs;
            System.Diagnostics.Debug.WriteLine(DLLPath);

            //Copy DLLs from app extension folder to local AppData folder
            //as the app extension folder does not have execute privileges
            string destFile = CopyDLLToAppDataFolder(DLLPath);

            // Load library wrapper
            Lib1Wrapper lib1 = new Lib1Wrapper(destFile);

            // Get version, 32-bit or 64-bit
            var version = lib1.GetString();

            Console.WriteLine(version);
            DLLNameLbl.Text = version;
        }

        string CopyDLLToAppDataFolder(string source)
        {
            string destFile = null;

            if (System.IO.File.Exists(source))
            {
                string fileName = System.IO.Path.GetFileName(source);
                destFile = appdatafolderpath + @"\" + fileName;

                System.IO.File.Copy(source, destFile, true);
            }
            else
            {
                Console.WriteLine("Source file does not exist!");
            }

            return destFile;
        }
    }

    class ExtensionManager
    {
        public class Extension
        {
            private AppExtension _extension;
            private string _serviceName;
            private string _uniqueId;
            private BitmapImage _logo;
            private string fullDLLPath;
            private readonly object _sync = new object();
            public event PropertyChangedEventHandler PropertyChanged;

            public Extension(AppExtension ext, PropertySet properties, BitmapImage logo)
            {
                _extension = ext;
                _logo = logo;

                _serviceName = null;

                //AUMID + Extension ID is the unique identifier for an extension
                _uniqueId = ext.AppInfo.AppUserModelId + "!" + ext.Id;
            }

            public BitmapImage Logo
            {
                get { return _logo; }
            }

            public string UniqueId
            {
                get { return _uniqueId; }
            }

            public AppExtension AppExtension
            {
                get { return _extension; }
            }

            // this controls loading of the extension
            public async Task Load()
            {
                #region Error Checking

                // make sure package is OK to load
                if (!_extension.Package.Status.VerifyIsOK())
                {
                    return;
                }
                #endregion
            }
        }

        private ObservableCollection<Extension> _extensions;
        private string _contract;
        private AppExtensionCatalog _catalog;
        public IReadOnlyList<AppExtension> extensions;
        String pathToDLLs = String.Empty;
        public ExtensionManager(string contract)
        {
            // extensions list   
            _extensions = new ObservableCollection<Extension>();

            // catalog & contract
            _contract = contract;
            _catalog = AppExtensionCatalog.Open(_contract);
        }

        public ObservableCollection<Extension> Extensions
        {
            get { return _extensions; }
        }

        public string Contract
        {
            get { return _contract; }
        }

        public async Task<String> Initialize()
        {
            // set up extension management events
            _catalog.PackageInstalled += Catalog_PackageInstalled;
            _catalog.PackageUpdated += Catalog_PackageUpdated;
            _catalog.PackageUninstalling += Catalog_PackageUninstalling;
            _catalog.PackageUpdating += Catalog_PackageUpdating;
            _catalog.PackageStatusChanged += Catalog_PackageStatusChanged;

            pathToDLLs = String.Empty;

            // Scan all extensions
            await FindAllExtensions();

            return pathToDLLs;
        }

        public async Task FindAllExtensions()
        {
            // load all the extensions currently installed
            extensions = await _catalog.FindAllAsync();
            foreach (AppExtension ext in extensions)
            {
                // load this extension
                await LoadExtension(ext);
                StorageFolder folder = await ext.GetPublicFolderAsync();
                pathToDLLs += folder.Path + "\\" + "Lib1.dll";
            }
        }

        private async void Catalog_PackageInstalled(AppExtensionCatalog sender, AppExtensionPackageInstalledEventArgs args)
        {
        }

        // package has been updated, so reload the extensions
        private async void Catalog_PackageUpdated(AppExtensionCatalog sender, AppExtensionPackageUpdatedEventArgs args)
        {
        }

        // package is updating, so just unload the extensions
        private async void Catalog_PackageUpdating(AppExtensionCatalog sender, AppExtensionPackageUpdatingEventArgs args)
        {
            await UnloadExtensions(args.Package);
        }

        // package is removed, so unload all the extensions in the package and remove it
        private async void Catalog_PackageUninstalling(AppExtensionCatalog sender, AppExtensionPackageUninstallingEventArgs args)
        {
            await RemoveExtensions(args.Package);
        }

        // package status has changed, could be invalid, licensing issue, app was on USB and removed, etc
        private async void Catalog_PackageStatusChanged(AppExtensionCatalog sender, AppExtensionPackageStatusChangedEventArgs args)
        {
            // get package status
            if (!(args.Package.Status.VerifyIsOK()))
            {
                // if it's offline unload only
                if (args.Package.Status.PackageOffline)
                {
                    await UnloadExtensions(args.Package);
                }

                // package is being serviced or deployed
                else if (args.Package.Status.Servicing || args.Package.Status.DeploymentInProgress)
                {
                    // ignore these package status events
                }

                // package is tampered or invalid or some other issue
                // glyphing the extensions would be a good user experience
                else
                {
                    await RemoveExtensions(args.Package);
                }

            }
            // if package is now OK, attempt to load the extensions
            else
            {
                // try to load any extensions associated with this package
                await LoadExtensions(args.Package);
            }
        }

        // loads an extension
        public async Task LoadExtension(AppExtension ext)
        {
            // get unique identifier for this extension
            string identifier = ext.AppInfo.AppUserModelId + "!" + ext.Id;

            // load the extension if the package is OK
            if (!(ext.Package.Status.VerifyIsOK()
                    // This is where we'd normally do signature verfication, but for demo purposes it isn't important
                    // Below is an example of how you'd ensure that you only load store-signed extensions :)
                    //&& ext.Package.SignatureKind == PackageSignatureKind.Store
                    ))
            {
                // if this package doesn't meet our requirements
                // ignore it and return
                return;
            }

            // if its already existing then this is an update
            Extension existingExt = _extensions.Where(e => e.UniqueId == identifier).FirstOrDefault();

            // new extension
            if (existingExt == null)
            {
                // get extension properties
                var properties = await ext.GetExtensionPropertiesAsync() as PropertySet;

                // create new extension
                Extension nExt = new Extension(ext, properties, null); //logo);

                // Add it to extension list
                _extensions.Add(nExt);

                // load it
                await nExt.Load();
            }
            // update
            else
            {
                ;
            }
        }

        // loads all extensions associated with a package - used for when package status comes back
        public async Task LoadExtensions(Package package)
        {
        }

        // unloads all extensions associated with a package - used for updating and when package status goes away
        public async Task UnloadExtensions(Package package)
        {
        }

        // removes all extensions associated with a package - used when removing a package or it becomes invalid
        public async Task RemoveExtensions(Package package)
        {
        }


        public async void RemoveExtension(Extension ext)
        {
            await _catalog.RequestRemovePackageAsync(ext.AppExtension.Package.Id.FullName);
        }

        #region Extra exceptions
        // For exceptions using the Extension Manager
        public class ExtensionManagerException : Exception
        {
            public ExtensionManagerException() { }

            public ExtensionManagerException(string message) : base(message) { }

            public ExtensionManagerException(string message, Exception inner) : base(message, inner) { }
        }
        #endregion
    }
}
