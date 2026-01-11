using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Speech.Synthesis; // Requires System.Speech reference
// using System.Speech.Recognition; 

namespace WotlkClient.Clients
{
    public class VoiceMgr
    {
        private SpeechSynthesizer synth;
        public bool VoiceEnabled { get; set; } = false;

        public VoiceMgr()
        {
            try
            {
                synth = new SpeechSynthesizer();
                synth.SetOutputToDefaultAudioDevice();
                // Select a french voice if possible
                foreach (var v in synth.GetInstalledVoices())
                {
                    if (v.VoiceInfo.Culture.Name.Contains("fr"))
                    {
                        synth.SelectVoice(v.VoiceInfo.Name);
                        break;
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("[Voice] Init Failed: " + ex.Message);
            }
        }

        public void Speak(string text)
        {
            if (!VoiceEnabled || synth == null) return;
            
            // Speak async (Standard)
            try
            {
                synth.SpeakAsync(text);
            }
            catch (Exception ex)
            { 
                 Console.WriteLine("[Voice] Error: " + ex.Message);
            }
        }
    }
}
