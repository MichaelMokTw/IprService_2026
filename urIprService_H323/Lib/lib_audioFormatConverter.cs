
namespace MyProject.lib {

    public class AudioFormatConverter {        
        public static short MuLaw_Decode(byte ulaw) {
            const int ULAW_BIAS = 0x84;
            int t;
            ulaw = (byte)~ulaw;
            t = (((ulaw & 0x0F) << 3) + ULAW_BIAS) << (((int)ulaw & 0x70) >> 4);
            return (short)((ulaw & 0x80) != 0 ? (ULAW_BIAS - t) : (t - ULAW_BIAS));
        }

        public static byte[] MuLawToPCM(byte[] buffer) {
            using (MemoryStream m = new MemoryStream()) {
                using (BinaryWriter bw = new BinaryWriter(m)) {
                    foreach (byte b in buffer) {
                        bw.Write(MuLaw_Decode(b));
                    }
                }
                //File.WriteAllBytes("out.raw", m.ToArray());
                return m.ToArray();
            }
        }

        // TEST
        // 每個 byte 轉換處理
        //byte[] l_Buffer = File.ReadAllBytes("in.raw");
        //using (MemoryStream m = new MemoryStream())
        //{
        //    using (BinaryWriter bw = new BinaryWriter(m)) {
        //        foreach (byte b in l_Buffer)
        //        {
        //            bw.Write(MuLaw_Decode(b));
        //        }
        //    }
        //    File.WriteAllBytes("out.raw", m.ToArray());
        //}
    }
    
}


