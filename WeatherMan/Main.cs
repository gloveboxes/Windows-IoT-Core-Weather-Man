using AdapterLib;
using Microsoft.Maker.Media.UniversalMediaEngine;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;

namespace HeadlessAdapterApp
{
    class Main : Banner
    {
        public async void Initialise()
        { 
            await InitSpeech();

            InitAllJoyn();

            InitBanner();  // initalised async
      

            adapter.AllJoynMethod += Adapter_AllJoynMethod;
        }

        public override void Adapter_AllJoynMethod(object sender, AllJoynMethodData e)
        {
            switch (e.Method.Name.ToLower())
            {
                case "joke":
                    var p = e.AdapterDevice.Properties.Where(x => x.Name == "Speech")?.First()
                       ?.Attributes?.Where(y => y.Value.Name == "Message")?.First();

                    if (p != null) { Speak(p.Value.Data as string); }
                    break;

                default:
                    break;
            }

            base.Adapter_AllJoynMethod(sender, e);
        }
    }
}
