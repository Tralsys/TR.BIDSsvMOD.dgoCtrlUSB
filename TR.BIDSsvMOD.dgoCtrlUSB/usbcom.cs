using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace TR.BIDSsvMOD.dgoCtrlUSB
{
  //Ref : https://www.ipentec.com/document/libusbdotnet-app-create

  public enum DeviceNameList
  {
    None, type2, shinkansen, ryojo, ryojo_ub, mtc_p5b8, mtc_p5b6, mtc_p4b8, mtc_p4B8_tq, mtc_p13b8
  }
  static public class UDevs
  {
    static public bool[] IsConnected = new bool[uDevFinder.Length];
    static public UsbDeviceFinder[] uDevFinder = new UsbDeviceFinder[]
    {
      new UsbDeviceFinder(0x0AE4, 0x0004),//type2
      new UsbDeviceFinder(0x0AE4, 0x0005),//Shinkansen
      new UsbDeviceFinder(0x0AE4, 0x0007),//ryojo
      new UsbDeviceFinder(0x0AE4, 0x0008),//ryojo_unbalance
      new UsbDeviceFinder(0x0AE4, 0x0101),//mtc_p5b8
      new UsbDeviceFinder(0x1C06, 0x77A7),//mtc_p5b6
      new UsbDeviceFinder(0x0000, 0x0000),//mtc_p4b8
      new UsbDeviceFinder(0x0000, 0x0000),//mtc_p4b8_tq
      new UsbDeviceFinder(0x0000, 0x0000) //mtc_p13b8
    };
  }
  public class DataGotEvArgs : EventArgs
  {
    public byte[] Data;
  }


  internal class Usbcom : IDisposable
  {
    private bool IsOpen = false;
    private UsbDevice uDevice;

    public DeviceNameList DevType { get; private set; } = DeviceNameList.None;
    
    public bool Connect()
    {
      if (!UsbFndCnt()) return false;
      
      (new Thread(() => {
        while (uDevice.IsOpen)
        {
          byte[] buf = new byte[8];
          int bytesRead = 0;

          using (var Reader = uDevice.OpenEndpointReader(ReadEndpointID.Ep01))
          {
            ErrorCode ec = ErrorCode.None;
            try
            {
              ec = Reader.Read(buf, 2000, out bytesRead);
            }
            catch (ObjectDisposedException) { return; }

            if (ec == ErrorCode.IoTimedOut) continue;
            if (ec == ErrorCode.IoCancelled) return;
            if (ec != 0) throw new Exception("usbcom ReadCtrl Error : ErrorCode==" + ec.ToString() + "\n" + UsbDevice.LastErrorString);
          }

          if (bytesRead > 0) (new Thread(() => DataGot?.Invoke(null, new DataGotEvArgs() { Data = buf }))).Start();
        }
      })).Start();
      return true;
    }

    private bool UsbFndCnt()
    {
      try
      {
        for (int i = 0; i < (int)DeviceNameList.mtc_p5b6; i++)
        {
          if (UDevs.IsConnected[i]) continue;
          try
          {
            uDevice = UsbDevice.OpenUsbDevice(UDevs.uDevFinder[i]);
          }
          catch (Exception e)
          {
            Console.WriteLine("usbcom Opening Process : {0}", e);
            throw;
          }
          if (uDevice == null) continue;
          DevType = (DeviceNameList)i + 1;
          UDevs.IsConnected[i] = IsOpen = true;
          break;
        }

        if (uDevice == null)
        {
          Console.WriteLine("Usb Finding Process : Device not found.");
          return false;
        }
        Console.WriteLine("Device was found.  DevType is {0} (VID : {1}, PID : {2})", DevType, uDevice.UsbRegistryInfo.Vid, uDevice.UsbRegistryInfo.Pid);

        IUsbDevice iuDev = uDevice as IUsbDevice;
        if (!ReferenceEquals(iuDev, null))
        {
          iuDev.SetConfiguration(1);
          iuDev.ClaimInterface(0);
        }

        uDevice?.Open();
      }
      catch (Exception e)
      {
        Console.WriteLine("usbcom Connecting Process : {0}", e);
        throw;
      }
      return true;
    }

    public void SendCtrl(byte[] Data, bool RawMode = false)
    {
      int transfered = 0;
      var usp = new UsbSetupPacket(0x40, 0x09, 0x0301, 0x0000, 8);
      try
      {
        if (uDevice?.IsOpen == true) uDevice.ControlTransfer(ref usp, Data, 8, out transfered);
      }
      catch (ObjectDisposedException) { return; }
    }

    public event EventHandler<DataGotEvArgs> DataGot;

    public void Close()
    {
      if (uDevice?.IsOpen == true) uDevice?.Close();
      if (IsOpen) UDevs.IsConnected[(int)DevType - 1] = IsOpen = false;
    }

    #region IDisposable Support
    private bool disposedValue = false; // 重複する呼び出しを検出するには

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          // TODO: マネージ状態を破棄します (マネージ オブジェクト)。
        }

        // TODO: アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
        // TODO: 大きなフィールドを null に設定します。

        disposedValue = true;
      }
    }

    // TODO: 上の Dispose(bool disposing) にアンマネージ リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
    // ~Usbcom()
    // {
    //   // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
    //   Dispose(false);
    // }

    // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
    public void Dispose()
    {
      // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
      Dispose(true);
      // TODO: 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
      // GC.SuppressFinalize(this);
    }
    #endregion
  }
}
