using System;
using TR.BIDSSMemLib;
using TR.BIDSsv;

namespace TR.BIDSsvMOD.dgoCtrlUSB
{
  public class BsvIF : IBIDSsv
  {
    public int Version => 103;
    public string Name { get; set; } = "dgoc2bids";
    public bool IsDebug { get; set; } = false;

    private CtrlCnv CC = new CtrlCnv();
    private Usbcom UC = new Usbcom();

    public bool Connect(in string args)
    {
      string[] sa = args.Replace(" ", string.Empty).ToLower().Split(new string[2] { "-", "/" }, StringSplitOptions.RemoveEmptyEntries);
      if (sa.Length > 1)
      {
        for (int i = 0; i < CC.BTIndex.Length; i++) CC.BTIndex[i] = null;
        for (int i = 1; i < sa.Length; i++)
        {
          string[] saa = sa[i].Split(':');
          try
          {
            if (saa.Length >= 1)
            {
              switch (saa[0])
              {
                case "a":
                  if (saa.Length > 1) CC.ATCPnlInd = int.Parse(saa[1]);
                  break;
                case "m":
                  if (saa.Length > 1) CC.MultiplNum = double.Parse(saa[1]);
                  break;
                case "n":
                  if (saa.Length > 1) Name = saa[1];
                  break;
                case "pbexc":
                  CC.isPBExc = true;
                  break;
                case "inv":
                  CC.isInv = true;
                  break;
                case "pcap":
                  CC.PCap = true;
                  break;
                default:
                  if (saa[0].StartsWith("bt") && saa.Length == 2)
                  {
                    string btN = saa[0].Remove(0, 2);
                    int btNAss = int.Parse(saa[1]);
                    if (btNAss >= Common.Ctrl_Key.Length) continue;
                    switch (btN)
                    {
                      case "start":
                        CC.BTIndex[(int)CtrlerKeysEN.Start] = btNAss;
                        break;
                      case "select":
                        CC.BTIndex[(int)CtrlerKeysEN.Select] = btNAss;
                        break;
                      case "s":
                        CC.BTIndex[(int)CtrlerKeysEN.S] = btNAss;
                        break;
                      case "a":
                        CC.BTIndex[(int)CtrlerKeysEN.A] = btNAss;
                        break;
                      case "aplus":
                        CC.BTIndex[(int)CtrlerKeysEN.APlus] = btNAss;
                        break;
                      case "b":
                        CC.BTIndex[(int)CtrlerKeysEN.B] = btNAss;
                        break;
                      case "c":
                        CC.BTIndex[(int)CtrlerKeysEN.C] = btNAss;
                        break;
                      case "d":
                        CC.BTIndex[(int)CtrlerKeysEN.D] = btNAss;
                        break;
                      case "up":
                        CC.BTIndex[(int)CtrlerKeysEN.Up] = btNAss;
                        break;
                      case "right":
                        CC.BTIndex[(int)CtrlerKeysEN.Right] = btNAss;
                        break;
                      case "down":
                        CC.BTIndex[(int)CtrlerKeysEN.Down] = btNAss;
                        break;
                      case "left":
                        CC.BTIndex[(int)CtrlerKeysEN.Left] = btNAss;
                        break;
                      case "horn":
                        CC.BTIndex[(int)CtrlerKeysEN.Horn] = btNAss;
                        break;
                    }
                  }
                  break;
              }
            }
          }
          catch (Exception e) { Console.WriteLine("dgo2bids Connect() : {0}", e); }
        }
      }

      
      UC.Connect();
      UC.DataGot += Usbcom_DataGot;

      switch (UC.DevType)
      {
        case DeviceNameList.shinkansen:
          CC.PHandleMAX = 13;
          CC.BHandleMAX = 8;
          break;
        case DeviceNameList.type2:
          CC.PHandleMAX = 5;
          CC.BHandleMAX = 9;
          break;
        case DeviceNameList.ryojo:
          //PHandleMAX = 13;
          //BHandleMAX = 8;
          break;
        case DeviceNameList.mtc_p5b8:
          CC.PHandleMAX = 5;
          CC.BHandleMAX = 8;
          break;
        case DeviceNameList.mtc_p5b6:
          CC.PHandleMAX = 5;
          CC.BHandleMAX = 6;
          break;
      }
      return true;
    }

    private void Usbcom_DataGot(object sender, DataGotEvArgs e)
    {
      CC.DataGot(e.Data, UC.DevType);
    }

    public void OnBSMDChanged(in BIDSSharedMemoryData data)
    {
      CC.OnBSMDChanged(data, UC.DevType);
    }

    public void OnOpenDChanged(in OpenD data) { }

    public void OnPanelDChanged(in int[] data)
    {
      CC.OnPanelDChanged(data, UC.DevType);
    }

    public void OnSoundDChanged(in int[] data) { }


    public void Print(in string data) { }
    public void Print(in byte[] data) { }

    public void WriteHelp(in string args)
    {
      Console.WriteLine("communicate with dgocon\n" +
        " -a : (atc) set the atc panel number (default : null)" +
        " -btXXX : (brake) assign the work to button (default : null)(XXX:start, select etc)" +
        " -m : (magnification) set the magnification number that multiply with ATC Panel value" +
        " -n : (name) set the name of this connection"+
        " -pbexc : (Power / Brake Exchange) Exchange roles of Power Handle and Brake Handle"+
        " -pcap : Only do Packet Capture" +
        " -inv : (inverse) Inverse the Handle Position Counting");
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
    // ~Main()
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
