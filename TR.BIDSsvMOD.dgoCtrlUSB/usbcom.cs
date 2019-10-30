using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace TR.BIDSsvMOD.dgoCtrlUSB
{
  //Ref : https://www.ipentec.com/document/libusbdotnet-app-create

  class Usbcom
  {
    static UsbDevice uDevice;
    //static UsbDeviceFinder uDevFinder = new UsbDeviceFinder(0x0AE4, 0x0005);
    static UsbDeviceFinder[] uDevFinder = new UsbDeviceFinder[8]
    {
      new UsbDeviceFinder(0x0AE4, 0x0005),//Shinkansen
      new UsbDeviceFinder(0x0AE4, 0x0004),//type2
      new UsbDeviceFinder(0x0AE4, 0x0007),//ryojo
      new UsbDeviceFinder(0x0AE4, 0x0101),//mtc_p5b8
      new UsbDeviceFinder(0x1C06, 0x77A7),//mtc_p5b6
      new UsbDeviceFinder(0x0000, 0x0000),//mtc_p4b8
      new UsbDeviceFinder(0x0000, 0x0000),//mtc_p4b8_tq
      new UsbDeviceFinder(0x0000, 0x0000) //mtc_p13b8
    };


    public enum DeviceNameList
    {
      None, shinkansen, type2, ryojo, mtc_p5b8, mtc_p5b6, mtc_p4b8, mtc_p4B8_tq, mtc_p13b8
    }

    static public DeviceNameList DevType { get; private set; } = DeviceNameList.None;
    
    public static void Connect()
    {
      try
      {
        for(int i = 0; i < (int)DeviceNameList.mtc_p5b6; i++)
        {
          uDevice = UsbDevice.OpenUsbDevice(uDevFinder[i]);
          if (uDevice == null) continue;
          DevType = (DeviceNameList)i + 1;
        }

        IUsbDevice iuDev = uDevice as IUsbDevice;
        if (!ReferenceEquals(iuDev, null))
        {
          iuDev.SetConfiguration(1);
          iuDev.ClaimInterface(0);
        }
        uDevice.Open();
        
      }
      catch(Exception e)
      {
        Console.WriteLine("usbcom Connecting Proces : {0}", e);
        throw;
      }
      (new Thread(() => {
        while (uDevice.IsOpen)
        {
          byte[] buf = new byte[64];
          int bytesRead = 0;

          using (var Reader = uDevice.OpenEndpointReader(ReadEndpointID.Ep01))
          {
            ErrorCode ec = ErrorCode.None;
            ec = Reader.Read(buf, 2000, out bytesRead);

            if (ec == ErrorCode.IoTimedOut) continue;
            if (ec == ErrorCode.IoCancelled) return;
            if (ec != 0) throw new Exception("usbcom ReadCtrl Error : ErrorCode==" + ec.ToString() + "\n" + UsbDevice.LastErrorString);
          }

          if (bytesRead > 0) (new Thread(() => DataGot?.Invoke(null, new DataGotEvArgs() { Data = buf }))).Start();
        }
      })).Start();
    }

    public static void SendCtrl(byte[] Data, bool RawMode = false)
    {
      int transfered = 0;
      var usp = new UsbSetupPacket(0x40, 0x09, 0x0301, 0x0000, 8);
      uDevice.ControlTransfer(ref usp, Data, 8, out transfered);
    }

    public class DataGotEvArgs : EventArgs
    {
      //public long TimeStamp;
      public byte[] Data;
    }
    public static event EventHandler<DataGotEvArgs> DataGot;

    public static void Close()
    {
      uDevice.Close();
    }
  }
}
