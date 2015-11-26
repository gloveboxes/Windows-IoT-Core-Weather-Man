using Glovebox.Graphics.Components;
using Glovebox.Graphics.Drivers;
using Glovebox.IoT.Devices.Converters;
using Glovebox.IoT.Devices.Sensors;
using System;
using System.Threading.Tasks;

namespace HeadlessAdapterApp
{
    class Banner : Speech
    {

        LED8x8Matrix matrix;
        LED8x8Matrix strip;

        // sensors
        BMP180 bmp180;

        Ldr light = null;

        //ADC
        AdcProviderManager adcManager;


        protected async void InitBanner()
        {
            matrix = new LED8x8Matrix(new Ht16K33());
            strip = new LED8x8Matrix(new MAX7219(4, MAX7219.Rotate.None, MAX7219.Transform.HorizontalFlip));

            matrix.SetBrightness(1);
            strip.SetBrightness(1);

            bmp180 = new BMP180(); // init temp and air pressure
            await InitLightAdc();  // init ldr on ads1015 adc


            ShowTempPressure();
            ShowLightLevel();

        }

        private async Task InitLightAdc()
        {
            adcManager = new AdcProviderManager();
            adcManager.Providers.Add(new ADS1015(ADS1015.Gain.Volt5));
            var ads1015 = (await adcManager.GetControllersAsync())[0];
            light = new Ldr(ads1015.OpenChannel(2));
        }

        async void ShowTempPressure()
        {
            double temperature = 0;
            int oldTemp = 0;
            DateTime lastSpoke = DateTime.Now;

            while (true)
            {
                temperature = bmp180.Temperature.DegreesCelsius;

                if ((int)temperature != oldTemp && lastSpoke.AddMinutes(1) > DateTime.Now)
                {
                    Speak("The temperature is now " + Math.Round(temperature, 1) + " degrees celsius");
                    oldTemp = (int)temperature;
                    lastSpoke = DateTime.Now;
                }

                string msg = string.Format("{0}, {1}C, {2}hPa, {3} ", preMessage, Math.Round(temperature, 1), Math.Round(bmp180.Pressure.Hectopascals, 0), postMessage);
                strip.ScrollStringInFromRight(msg, 80);
                await Task.Delay(10);
            }
        }

        async void ShowLightLevel()
        {
            double lvl = 0;

            while (true)
            {
                lvl = light.ReadRatio * 100;
                //if (lvl < 50)
                //{
                //    Speak("It's dark in here!");
                //}

                string lightMsg = string.Format("{0}p ", Math.Round(lvl, 1));
                matrix.ScrollStringInFromRight(lightMsg, 100);
                await Task.Delay(10);
            }
        }
    }
}
