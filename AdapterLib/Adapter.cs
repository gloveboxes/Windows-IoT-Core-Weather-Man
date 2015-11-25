using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using BridgeRT;
using Windows.Foundation;

namespace AdapterLib
{



    public delegate void AllJoynSetEventHandler(object sender, AllJoynData e);
    public delegate void AllJoynGetEventHandler(object sender, AllJoynAttributeData e);
    public delegate void AllJoynMethodEventHandler(object sender, AllJoynMethodData e);


    public sealed class AllJoynMethodData
    {
        public IAdapterMethod Method { get; private set; }
        public IAdapterDevice AdapterDevice { get; private set; }

        public AllJoynMethodData(IAdapterMethod method, IAdapterDevice adapterDevice)
        {
            this.Method = method;
            this.AdapterDevice = adapterDevice;
        }
    }

    public sealed class AllJoynData
    {
        public IAdapterValue Value { get; private set; }

        public AllJoynData(IAdapterValue Value)
        {
            this.Value = Value;
        }
    }

    public sealed class AllJoynAttributeData
    {
        public string name { get; private set; }
        public object value { get; set; }

        public AllJoynAttributeData(string name)
        {
            this.name = name;
        }
    }


    public sealed class Adapter : IAdapter
    {


        public event AllJoynSetEventHandler AllJoynSet;
        public event AllJoynGetEventHandler AllJoynGet;
        public event AllJoynMethodEventHandler AllJoynMethod;


        private const uint ERROR_SUCCESS = 0;
        private const uint ERROR_INVALID_HANDLE = 6;

        // Device Arrival and Device Removal Signal Indices
        private const int DEVICE_ARRIVAL_SIGNAL_INDEX = 0;
        private const int DEVICE_ARRIVAL_SIGNAL_PARAM_INDEX = 0;
        private const int DEVICE_REMOVAL_SIGNAL_INDEX = 1;
        private const int DEVICE_REMOVAL_SIGNAL_PARAM_INDEX = 0;


        // GPIO Device
        private string DeviceName = GetDeviceName(); // "RPi02";
        private const string VENDOR = "Glovebox";
        private const string MODEL = "MakerDen";
        private const string VERSION = "1.0.0.0";
        private const string SERIAL_NUMBER = "1111111111111";
        private const string DESCRIPTION = "Maker Den Device";


        public string Vendor { get; }

        public string AdapterName { get; }

        public string Version { get; }

        public string ExposedAdapterPrefix { get; }

        public string ExposedApplicationName { get; }

        public Guid ExposedApplicationGuid { get; }

        public IList<IAdapterSignal> Signals { get; }

        public Adapter()
        {
            Windows.ApplicationModel.Package package = Windows.ApplicationModel.Package.Current;
            Windows.ApplicationModel.PackageId packageId = package.Id;
            Windows.ApplicationModel.PackageVersion versionFromPkg = packageId.Version;

            this.DeviceName = GetDeviceName();
            this.Vendor = "glovebox";
            this.AdapterName = "makerden";

            // the adapter prefix must be something like "com.mycompany" (only alpha num and dots)
            // it is used by the Device System Bridge as root string for all services and interfaces it exposes
            this.ExposedAdapterPrefix = "com." + this.Vendor.ToLower();
            this.ExposedApplicationGuid = Guid.Parse("{0xc1e7ce4a,0x66fb,0x40e6,{0xb4,0xb7,0x74,0x2b,0x88,0x71,0x83,0x27}}");

            if (null != package && null != packageId)
            {
                this.ExposedApplicationName = packageId.Name;
                this.Version = versionFromPkg.Major.ToString() + "." +
                               versionFromPkg.Minor.ToString() + "." +
                               versionFromPkg.Revision.ToString() + "." +
                               versionFromPkg.Build.ToString();
            }
            else
            {
                this.ExposedApplicationName = "DeviceSystemBridge";
                this.Version = "0.0.0.0";
            }

            try
            {
                this.Signals = new List<IAdapterSignal>();
                this.devices = new List<IAdapterDevice>();
                this.signalListeners = new Dictionary<int, IList<SIGNAL_LISTENER_ENTRY>>();

                //Create Adapter Signals
                this.createSignals();
            }
            catch (OutOfMemoryException ex)
            {
                throw;
            }
        }

        #region updated template code by dglover@microsoft.com


        public static string GetDeviceName()
        {
            var hostNamesList = Windows.Networking.Connectivity.NetworkInformation.GetHostNames();
            return hostNamesList.Where(x => x.Type == Windows.Networking.HostNameType.DomainName).FirstOrDefault().CanonicalName.Split('.')[0].ToUpper() + "_AllJoyn";
        }

        public uint Initialize()
        {
            AdapterDevice myDevice = new AdapterDevice(DeviceName, VENDOR, MODEL, VERSION, SERIAL_NUMBER, DESCRIPTION);

            AdapterProperty robotProperty = new AdapterProperty("Robot", "");
            robotProperty.Attributes.Add(NewAttribute("Mode", "manual", E_ACCESS_TYPE.ACCESS_READWRITE));
            robotProperty.Attributes.Add(NewAttribute("Direction", 0, E_ACCESS_TYPE.ACCESS_READWRITE));
            robotProperty.Attributes.Add(NewAttribute("Speed", 5, E_ACCESS_TYPE.ACCESS_READWRITE));
            myDevice.Properties.Add(robotProperty);


            myDevice.Methods.Add(new AdapterMethod("stop", "Stop", 0));
            myDevice.Methods.Add(new AdapterMethod("forward", "Forward", 0));
            myDevice.Methods.Add(new AdapterMethod("left", "Turn left", 0));
            myDevice.Methods.Add(new AdapterMethod("right", "Turn right", 0));
            myDevice.Methods.Add(new AdapterMethod("backward", "Backwards", 0));
            myDevice.Methods.Add(new AdapterMethod("joke", "Tell me a joke", 0));
            myDevice.Methods.Add(new AdapterMethod("banner", "Banner Control", 0));


            AdapterProperty lightProperty = new AdapterProperty("Light", "");
            lightProperty.Attributes.Add(NewAttribute("Mode", "off", E_ACCESS_TYPE.ACCESS_READWRITE));
            myDevice.Properties.Add(lightProperty);


            AdapterProperty speechProperty = new AdapterProperty("Speech", "");
            speechProperty.Attributes.Add(NewAttribute("Volume", 4, E_ACCESS_TYPE.ACCESS_READWRITE));
            speechProperty.Attributes.Add(NewAttribute("Message", "Tell me a joke", E_ACCESS_TYPE.ACCESS_READWRITE));
            myDevice.Properties.Add(speechProperty);

            AdapterProperty bannerProperty = new AdapterProperty("Banner", "");
            bannerProperty.Attributes.Add(NewAttribute("Pre", "Maker Den", E_ACCESS_TYPE.ACCESS_READWRITE));
            bannerProperty.Attributes.Add(NewAttribute("Post", "Data Den", E_ACCESS_TYPE.ACCESS_READWRITE));
            bannerProperty.Attributes.Add(NewAttribute("Calibration", 0, E_ACCESS_TYPE.ACCESS_READWRITE));
            myDevice.Properties.Add(bannerProperty);


            devices.Add(myDevice);

            return ERROR_SUCCESS;
        }


        AdapterAttribute NewAttribute(string name, int value, E_ACCESS_TYPE access)
        {
            object data = PropertyValue.CreateInt32(value);
            return NewAttribute(name, data, access);
        }

        AdapterAttribute NewAttribute(string name, string value, E_ACCESS_TYPE access)
        {
            object data = PropertyValue.CreateString(value);
            return NewAttribute(name, data, access);
        }

        AdapterAttribute NewAttribute(string name, object value, E_ACCESS_TYPE access)
        {
            AdapterAttribute attr = new AdapterAttribute(name, value, access);
            attr.COVBehavior = SignalBehavior.Always;

            return attr;
        }


        public uint GetPropertyValue(IAdapterProperty Property, string AttributeName, out IAdapterValue ValuePtr, out IAdapterIoRequest RequestPtr)
        {
            ValuePtr = null;
            RequestPtr = null;

            var r = ((AdapterProperty)Property).Attributes.Where(x => x.Value.Name == AttributeName).First() as BridgeRT.IAdapterAttribute;

            if (r == null) { return ERROR_INVALID_HANDLE; }
            else
            {
                ValuePtr = r.Value;
                return ERROR_SUCCESS;
            }
        }


        public uint SetPropertyValue(IAdapterProperty Property, IAdapterValue Value, out IAdapterIoRequest RequestPtr)
        {
            RequestPtr = null;

            var r = ((AdapterProperty)Property).Attributes.Where(x => x.Value.Name == Value.Name).First() as BridgeRT.IAdapterAttribute;

            if (r == null) { return ERROR_INVALID_HANDLE; }
            else
            {
                r.Value.Data = Value.Data;

                AllJoynSet?.Invoke(this, new AllJoynData(r.Value));
                return ERROR_SUCCESS;
            }
        }

        public uint CallMethod(IAdapterMethod Method, out IAdapterIoRequest RequestPtr)
        {
            RequestPtr = null;


            AllJoynMethod?.Invoke(this, new AllJoynMethodData(Method, devices[0]));

            return ERROR_SUCCESS;
        }


        #endregion


        public uint SetConfiguration([ReadOnlyArray] byte[] ConfigurationData)
        {
            return ERROR_SUCCESS;
        }

        public uint GetConfiguration(out byte[] ConfigurationDataPtr)
        {
            ConfigurationDataPtr = null;

            return ERROR_SUCCESS;
        }



        public uint Shutdown()
        {
            return ERROR_SUCCESS;
        }

        public uint EnumDevices(
            ENUM_DEVICES_OPTIONS Options,
            out IList<IAdapterDevice> DeviceListPtr,
            out IAdapterIoRequest RequestPtr)
        {
            RequestPtr = null;

            try
            {
                DeviceListPtr = new List<IAdapterDevice>(this.devices);
            }
            catch (OutOfMemoryException ex)
            {
                throw;
            }

            return ERROR_SUCCESS;
        }

        public uint GetProperty(
            IAdapterProperty Property,
            out IAdapterIoRequest RequestPtr)
        {
            RequestPtr = null;

            return ERROR_SUCCESS;
        }

        public uint SetProperty(
            IAdapterProperty Property,
            out IAdapterIoRequest RequestPtr)
        {
            RequestPtr = null;

            return ERROR_SUCCESS;
        }




        public uint RegisterSignalListener(
            IAdapterSignal Signal,
            IAdapterSignalListener Listener,
            object ListenerContext)
        {
            if (Signal == null || Listener == null)
            {
                return ERROR_INVALID_HANDLE;
            }

            int signalHashCode = Signal.GetHashCode();

            SIGNAL_LISTENER_ENTRY newEntry;
            newEntry.Signal = Signal;
            newEntry.Listener = Listener;
            newEntry.Context = ListenerContext;

            lock (this.signalListeners)
            {
                if (this.signalListeners.ContainsKey(signalHashCode))
                {
                    this.signalListeners[signalHashCode].Add(newEntry);
                }
                else
                {
                    IList<SIGNAL_LISTENER_ENTRY> newEntryList;

                    try
                    {
                        newEntryList = new List<SIGNAL_LISTENER_ENTRY>();
                    }
                    catch (OutOfMemoryException ex)
                    {
                        throw;
                    }

                    newEntryList.Add(newEntry);
                    this.signalListeners.Add(signalHashCode, newEntryList);
                }
            }

            return ERROR_SUCCESS;
        }

        public uint UnregisterSignalListener(
            IAdapterSignal Signal,
            IAdapterSignalListener Listener)
        {
            return ERROR_SUCCESS;
        }

        public uint NotifySignalListener(IAdapterSignal Signal)
        {
            if (Signal == null)
            {
                return ERROR_INVALID_HANDLE;
            }

            int signalHashCode = Signal.GetHashCode();

            lock (this.signalListeners)
            {
                IList<SIGNAL_LISTENER_ENTRY> listenerList = this.signalListeners[signalHashCode];
                foreach (SIGNAL_LISTENER_ENTRY entry in listenerList)
                {
                    IAdapterSignalListener listener = entry.Listener;
                    object listenerContext = entry.Context;
                    listener.AdapterSignalHandler(Signal, listenerContext);
                }
            }

            return ERROR_SUCCESS;
        }

        public uint NotifyDeviceArrival(IAdapterDevice Device)
        {
            if (Device == null)
            {
                return ERROR_INVALID_HANDLE;
            }

            IAdapterSignal deviceArrivalSignal = this.Signals[DEVICE_ARRIVAL_SIGNAL_INDEX];
            IAdapterValue signalParam = deviceArrivalSignal.Params[DEVICE_ARRIVAL_SIGNAL_PARAM_INDEX];
            signalParam.Data = Device;
            this.NotifySignalListener(deviceArrivalSignal);

            return ERROR_SUCCESS;
        }

        public uint NotifyDeviceRemoval(IAdapterDevice Device)
        {
            if (Device == null)
            {
                return ERROR_INVALID_HANDLE;
            }

            IAdapterSignal deviceRemovalSignal = this.Signals[DEVICE_REMOVAL_SIGNAL_INDEX];
            IAdapterValue signalParam = deviceRemovalSignal.Params[DEVICE_REMOVAL_SIGNAL_PARAM_INDEX];
            signalParam.Data = Device;
            this.NotifySignalListener(deviceRemovalSignal);

            return ERROR_SUCCESS;
        }

        private void createSignals()
        {
            try
            {
                // Device Arrival Signal
                AdapterSignal deviceArrivalSignal = new AdapterSignal(Constants.DEVICE_ARRIVAL_SIGNAL);
                AdapterValue deviceHandle_arrival = new AdapterValue(
                                                            Constants.DEVICE_ARRIVAL__DEVICE_HANDLE,
                                                            null);
                deviceArrivalSignal.Params.Add(deviceHandle_arrival);

                // Device Removal Signal
                AdapterSignal deviceRemovalSignal = new AdapterSignal(Constants.DEVICE_REMOVAL_SIGNAL);
                AdapterValue deviceHandle_removal = new AdapterValue(
                                                            Constants.DEVICE_REMOVAL__DEVICE_HANDLE,
                                                            null);
                deviceRemovalSignal.Params.Add(deviceHandle_removal);

                // Add Signals to the Adapter Signals
                this.Signals.Add(deviceArrivalSignal);
                this.Signals.Add(deviceRemovalSignal);
            }
            catch (OutOfMemoryException ex)
            {
                throw;
            }
        }

        private struct SIGNAL_LISTENER_ENTRY
        {
            // The signal object
            internal IAdapterSignal Signal;

            // The listener object
            internal IAdapterSignalListener Listener;

            //
            // The listener context that will be
            // passed to the signal handler
            //
            internal object Context;
        }

        // List of Devices
        private IList<IAdapterDevice> devices;

        // A map of signal handle (object's hash code) and related listener entry
        private Dictionary<int, IList<SIGNAL_LISTENER_ENTRY>> signalListeners;
    }
}
