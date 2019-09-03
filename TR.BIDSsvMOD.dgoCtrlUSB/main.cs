using System;
using TR.BIDSSMemLib;
using TR.BIDSsv;

namespace TR.BIDSsvMOD.dgoCtrlUSB
{
  public class main : IBIDSsv
  {
    public int Version => 100;
    public string Name { get; set; } = "dgoc2bids";
    public bool IsDebug { get; set; } = false;

    const int PHandleMAX = 13;
    const int BHandleMAX = 8;

    int? ATCPnlInd = null;
    double MultiplNum = 0;
    bool isPBExc = false;

    public bool Connect(in string args)
    {
      string[] sa = args.Replace(" ", string.Empty).Split(new string[2] { "-", "/" }, StringSplitOptions.RemoveEmptyEntries);
      if (sa.Length > 1)
      {
        for (int i = 1; i < sa.Length; i++)
        {
          string[] saa = sa[i].Split(':');
          try
          {
            if (saa.Length > 1)
            {
              switch (saa[0])
              {
                case "a":
                  ATCPnlInd = int.Parse(saa[1]);
                  break;
                case "m":
                  MultiplNum = double.Parse(saa[1]);
                  break;
                case "n":
                  Name = saa[1];
                  break;
                case "pbexc":
                  isPBExc = true;
                  break;
              }
            }
          }
          catch (Exception e) { Console.WriteLine("dgo2bids Connect() : {0}", e); }
        }
      }

      
      usbcom.Connect(usbcom.DeviceNameList.TCPP20011);
      usbcom.DataGot += Usbcom_DataGot;
      return true;
    }

    private void Usbcom_DataGot(object sender, usbcom.DataGotEvArgs e)
    {
      if (IsDebug)
      {
        string s = string.Empty;
        for (int i = 0; i < e.Data.Length; i++) s += e.Data[i].ToString() + " ";
        Console.WriteLine("{0} << {1}", Name, s);
      }
      if (e.Data[0] != 0xff)
      {
        int BNum = (int)Math.Round(e.Data[0] / 28.0, MidpointRounding.AwayFromZero) - 1;
        if (isPBExc) Common.PowerNotchNum = BNum;
        else Common.BrakeNotchNum = BNum >= BHandleMAX ? 99 : BNum;
      }
      if (e.Data[1] != 0xff)
      {
        int PNum = (int)Math.Round(e.Data[1] / 18.0, MidpointRounding.AwayFromZero) - 1;
        if (isPBExc) Common.BrakeNotchNum = PNum >= PHandleMAX ? 99 : PNum;
        else Common.PowerNotchNum = PNum;
      }

      bool[] keyState = Common.Ctrl_Key;
      keyState[0] = e.Data[2] != 0xff;//Horn SW
      byte buf = e.Data[4];
      keyState[8] = (buf & 1) == 1;
      keyState[7] = ((buf >>= 1) & 1) == 1;
      keyState[6] = ((buf >>= 1) & 1) == 1;
      keyState[5] = ((buf >>= 1) & 1) == 1;
      keyState[9] = ((buf >>= 1) & 1) == 1;
      keyState[4] = ((buf >>= 1) & 1) == 1;

      keyState[10] = false;
      keyState[11] = false;
      switch (e.Data[3] / 2)
      {
        case 0://^
          Common.ReverserNum = Common.ReverserNum == 1 ? 0 : 1;
          break;
        case 1://->
          keyState[10] = true;
          break;
        case 2://V
          Common.ReverserNum = Common.ReverserNum == -1 ? 0 : -1;
          break;
        case 3://<-
          keyState[11] = true;
          break;
      }

      Common.Ctrl_Key = keyState;
    }

    public void Dispose()
    {
      usbcom.DataGot -= Usbcom_DataGot;
      usbcom.Close();
    }

    const double SPLEDSpdCount = 350 / 0x17;
    byte LEDBar = 0;
    short CurrentSPD = 0;
    short ATCSPD = 0;
    bool isDoorClosed = false;

    public void OnBSMDChanged(in BIDSSharedMemoryData data)
    {
      LEDBar = (byte)Math.Ceiling(Math.Abs(data.StateData.V / SPLEDSpdCount));
      CurrentSPD = (short)Math.Ceiling(Math.Abs(data.StateData.V));

      isDoorClosed = data.IsDoorClosed;

      UpdateDisplay();
    }

    private void UpdateDisplay()
    {
      int TenLEDSPDBar = ATCSPD - CurrentSPD;
      byte TenLEDSPDBar_print = (byte)((10 >= TenLEDSPDBar && TenLEDSPDBar > 0) ? TenLEDSPDBar : 0);

      if (isDoorClosed) TenLEDSPDBar_print |= 0b10000000;

      byte[] ba = new byte[8];

      ba[2] = TenLEDSPDBar_print;//Door, Difference bet ATC/CurrSPD
      ba[3] = LEDBar;

      ba[7] = (byte)Math.Floor((ATCSPD % 1000) / 100.0);
      byte buf = (byte)Math.Floor((ATCSPD % 100) / 10.0);
      buf <<= 4;
      buf += (byte)(ATCSPD % 10);
      ba[6] = buf;

      ba[5] = (byte)Math.Floor((CurrentSPD % 1000) / 100.0);
      buf = (byte)Math.Floor((CurrentSPD % 100) / 10.0);
      buf <<= 4;
      buf += (byte)(CurrentSPD % 10);
      ba[4] = buf;

      if (IsDebug)
      {
        string s = string.Empty;
        for (int i = 0; i < ba.Length; i++) s += ba[i].ToString() + " ";
        Console.WriteLine("{0} << {1}", Name, s);
      }
      usbcom.SendCtrl(ba);
    }

    public void OnOpenDChanged(in OpenD data) { }

    public void OnPanelDChanged(in int[] data) {
      if (ATCPnlInd != null && data?.Length > ATCPnlInd) 
      {
        short atcspdRec = ATCSPD;
        ATCSPD = (short)(data[ATCPnlInd ?? 0] * MultiplNum);
        if (atcspdRec != ATCSPD) UpdateDisplay();
      }
    }

    public void OnSoundDChanged(in int[] data) { }


    public void Print(in string data) { }
    public void Print(in byte[] data) { }

    public void WriteHelp(in string args)
    {
      Console.WriteLine("communicate with dgocon\n" +
        " -a : (atc) set the atc panel number (default : null)" +
        " -m : (magnification) set the magnification number that multiply with ATC Panel value"+
        " -n : (name) set the name of this connection"+
        " -pbexc : (Power / Brake Exchange) Exchange roles of Power Handle and Brake Handle");
    }
  }
}
