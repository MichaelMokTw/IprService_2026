using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MyProject.lib {
    public static class lib_volume {

        const int MAX_dB = 99;

        static double Log_dB(UInt64 v) {
            double d = v / (double)(0x8000 * 0x8000);
            if (v == 0)
                return MAX_dB;
            return -Math.Log10(d) * 10;
        }

        // 1. data 為 Voice RAW data,
        // 2. 可以塞入 
        // 2. 格式是未壓縮的 Signed 16-bit PCM(LPCM)
        //
        public static double? VolumeDetect(byte[] data) {
            double max;
            Int16 pcm;
            UInt64[] histogram = new UInt64[0x10001];

            using (MemoryStream l_Stream = new MemoryStream(data)) {
                while (l_Stream.Position < l_Stream.Length) {
                    pcm = (Int16)(l_Stream.ReadByte() | (l_Stream.ReadByte() << 8));
                    histogram[pcm + 0x8000]++;
                }
            }

            int i, max_volume, shift;
            UInt64 nb_samples = 0, nb_samples_shift = 0;

            for (i = 0; i < 0x10000; i++)
                nb_samples += histogram[i];
            if (nb_samples == 0)
                return null;
            shift = (int)Math.Log2(nb_samples >> 33);
            for (i = 0; i < 0x10000; i++)
                nb_samples_shift += histogram[i] >> shift;
            if (nb_samples_shift == 0)
                return null;
            max_volume = 0x8000;
            while (max_volume > 0 && (histogram[0x8000 + max_volume] == 0) && (histogram[0x8000 - max_volume] == 0))
                max_volume--;
            max = -Log_dB((UInt64)(max_volume * max_volume));
            
            return max;
        }
    }
}
