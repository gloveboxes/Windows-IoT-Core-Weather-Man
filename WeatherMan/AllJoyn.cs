using AdapterLib;
using BridgeRT;
using System;
using System.Linq;

namespace HeadlessAdapterApp
{
    class AllJoyn
    {

        //alljoyn
        protected Adapter adapter = null;
        private DsbBridge dsbBridge;

        protected string preMessage = "Maker Den";
        protected string postMessage = "Data Den";
        protected int calibration = 0;

        protected void InitAllJoyn() {

            try
            {
                adapter = new Adapter();
                dsbBridge = new DsbBridge(adapter);

                var initResult = dsbBridge.Initialize();
                if (initResult != 0)
                {
                    throw new Exception("DSB Bridge initialization failed!");
                }
            }
            catch (Exception ex)
            {
                if (dsbBridge != null)
                {
                    dsbBridge.Shutdown();
                }

                if (adapter != null)
                {
                    adapter.Shutdown();
                }

                throw;
            }

    
        }

        public virtual void Adapter_AllJoynMethod(object sender, AllJoynMethodData e)
        {
            switch (e.Method.Name.ToLower())
            {

                case "banner":
                    preMessage = e.AdapterDevice.Properties.Where(x => x.Name == "Banner").First()
                        .Attributes.Where(y => y.Value.Name == "Pre").First().Value.Data as string;

                    postMessage = e.AdapterDevice.Properties.Where(x => x.Name == "Banner").First()
                        .Attributes.Where(y => y.Value.Name == "Post").First().Value.Data as string;

                    var cal = e.AdapterDevice.Properties.Where(x => x.Name == "Banner").First()
                        .Attributes.Where(y => y.Value.Name == "Calibration").First().Value.Data;

                    calibration = (int)cal;

                    break;

                default:
                    break;
            }
        }
    }
}
