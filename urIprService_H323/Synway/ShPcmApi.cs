using System.Runtime.InteropServices;

namespace ShPcmHandle
{
	public class ShPcmApi
    {
    [DllImport("ShPcmHandle.dll")] public static extern int	fPcm_Mp3ConvertALaw(byte[] szSourceFile, byte[] szTargetFile);                                                 
    [DllImport("ShPcmHandle.dll")] public static extern int	fPcm_AdpcmToMp3(byte[] szSourceFile, byte[] szTargetFile);                                                     
    [DllImport("ShPcmHandle.dll")] public static extern int	fPcm_Mp3ConvertULaw(byte[] szSourceFile, byte[] szTargetFile);                                                 
    [DllImport("ShPcmHandle.dll")] public static extern uint	fPcm_MemMp3ToPcm16(byte[]pSource, uint dwSourceSize, byte[]pTarget, uint dwTargetSize);                    
    [DllImport("ShPcmHandle.dll")] public static extern int	fPcm_ALawConvertMp3(byte[] szSourceFile, byte[] szTargetFile);                                                     
    [DllImport("ShPcmHandle.dll")] public static extern uint fPcm_MemGSMToPcm16(byte[]pSource, uint dwSourceSize, byte[]pTarget, uint dwTargetSize);                      
    [DllImport("ShPcmHandle.dll")] public static extern int	fPcm_ULawConvertGSM(byte[] szSourceFile, byte[] szTargetFile);                                                     
    [DllImport("ShPcmHandle.dll")] public static extern int	fPcm_AdpcmToGsm(byte[] szSourceFile, byte[] szTargetFile);                                                     
                                                                                                                             
    [DllImport("ShPcmHandle.dll")] public static extern int fPcm_GetWaveFormat(byte[]szFileName);                                                                                 
    [DllImport("ShPcmHandle.dll")] public static extern int	fPcm_GetLastErrMsg(byte[] szErr);                                                                              
    [DllImport("ShPcmHandle.dll")] public static extern int	fPcm_ALawConvertPcm16(byte[] szSourceFile, byte[] szTargetFile);                                                   
    [DllImport("ShPcmHandle.dll")] public static extern int	fPcm_ALawConvertPcm8(byte[] szSourceFile, byte[] szTargetFile);                                                    
    [DllImport("ShPcmHandle.dll")] public static extern uint	fPcm_MemAdpcmToPcm16(byte[] pSource, uint dwSourceSize, byte[] pTarget, uint dwTargetSize);                
    [DllImport("ShPcmHandle.dll")] public static extern uint	fPcm_ConvertFskCID(byte[] pFSKBuf, int nFskLen, byte[]  pszCIDNumber, byte[] pszTime, byte[] pszName, int nMode);
    [DllImport("ShPcmHandle.dll")] public static extern uint fPcm_MemAdpcmToALAW(byte[] pSource, uint dwSourceSize, byte[] pTarget, uint dwTargetSize);                      
    [DllImport("ShPcmHandle.dll")] public static extern uint fPcm_MemAdpcmToULAW(byte[] pSource, uint dwSourceSize, byte[] pTarget, uint dwTargetSize);                      
    [DllImport("ShPcmHandle.dll")] public static extern uint	fPcm_MemPcm16ToAlaw(byte[] pSource, uint dwSourceSize, byte[] pTarget, uint dwTargetSize);                 
    [DllImport("ShPcmHandle.dll")] public static extern int	fPcm_AdpcmToAlaw(byte[]pSource, byte[] pTarget);                                                               
    [DllImport("ShPcmHandle.dll")] public static extern int	fPcm_GC8Convert(byte[] szSourceFile, byte[] szTargetFile, int nTargetFormat);                                  
    [DllImport("ShPcmHandle.dll")] public static extern int fPcm_Vox6KTo8K(byte[] szSourceFile, byte[] szTargetFile);                                                         
    [DllImport("ShPcmHandle.dll")] public static extern int fPcm_Vox8KTo6K(byte[] szSourceFile, byte[] szTargetFile);                                                         
    [DllImport("ShPcmHandle.dll")] public static extern int	fPcm_Pcm16ConvertALaw(byte[] szSourceFile, byte[] szTargetFile);                                               
    [DllImport("ShPcmHandle.dll")] public static extern int	fPCM_AlawConvertGC8 (byte[] szSoureFile, byte[]  szTargetFile);                                                          
    [DllImport("ShPcmHandle.dll")] public static extern int	fPcm_MemAlawToPcm8(byte[] szSource, int nSourceLen, byte[] szTarget, int nTargetLen);                          
    [DllImport("ShPcmHandle.dll")] public static extern int	fPcm_MemPcm8ToAlaw(byte[] szSource, int nSourceLen, byte[] szTarget, int nTargetLen);                          
    [DllImport("ShPcmHandle.dll")] public static extern int	fPcm_MemAlawToPcm16(byte[] szSource, int nSourceLen, byte[] szTarget, int nTargetLen);                         
    [DllImport("ShPcmHandle.dll")] public static extern int	fPcm_G729AConvert(byte[] szSourceFile, byte[] szTargetFile, int nTargetFormat);                                
                                                                                                                              
    [DllImport("ShPcmHandle.dll")] public static extern uint	fPcm_MemGSMToPcm8(byte[]pSource, uint dwSourceSize, byte[]pTarget, uint dwTargetSize);                                                                       
                                                                                                                             
    [DllImport("ShPcmHandle.dll")] public static extern int	fPCM_MemULawToG729A(byte[] szSource, int nSourceLen, byte[] szTarget, int nTargetLen);                                                                                                                          
    [DllImport("ShPcmHandle.dll")] public static extern int	fPcm_MemAlawToUlaw(byte[] szSource, int nSourceLen, byte[] szTarget, int nTargetLen);                             
    [DllImport("ShPcmHandle.dll")] public static extern int fPcm_MemUlawtoAlaw(byte[] szSource, int nSourceLen, byte[] szTarget, int nTargetLen);                             
    [DllImport("ShPcmHandle.dll")] public static extern int fPcm_UlawToAlaw(byte[] szSourceFile, byte[] szTargetFile);                                                         
    [DllImport("ShPcmHandle.dll")] public static extern int	fPcm_AlawToUlaw(byte[] szSourceFile, byte[] szTargetFile);                                                         
                                                                                                                                                                                     
    [DllImport("ShPcmHandle.dll")] public static extern void	fPcm_Close();                                                                                                
                                                                                                                             
    [DllImport("ShPcmHandle.dll")] public static extern  int	fPcm_ULawConvertMp3(byte[] szSourceFile, byte[] szTargetFile);                                                     
    [DllImport("ShPcmHandle.dll")] public static extern  int fPCM_Pcm16ConvertG729A(byte[] szSoureFile, byte[]  szTargetFile);                                                         

	}
}