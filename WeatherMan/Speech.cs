using Microsoft.Maker.Media.UniversalMediaEngine;
using System;
using System.Collections;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;

namespace HeadlessAdapterApp
{
    class Speech : AllJoyn
    {
        //Speech Synth
        private MediaEngine mediaEngine;
        SpeechSynthesizer synth;
        bool speaking = false;

        public Speech()
        {
            InitSpeech().Wait();
        }

        public async Task InitSpeech()
        {
            mediaEngine = new MediaEngine();
            synth = new SpeechSynthesizer();
            await mediaEngine.InitializeAsync();
        }

        protected async void Speak(string message)
        {
            if (speaking) { return; }
            speaking = true;
            SpeechSynthesisStream stream = await synth.SynthesizeTextToStreamAsync(message);
            mediaEngine.PlayStream(stream);
            await Task.Delay(1500);
            speaking = false;
        }
    }
}
