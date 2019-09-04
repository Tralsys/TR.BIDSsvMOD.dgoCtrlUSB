using System;
using TR.BIDSSMemLib;
using TR.BIDSsv;

namespace TR.BIDSsvMOD.dgoCtrlUSB
{
  public class main : IBIDSsv
  {
    public int Version => 101;
    public string Name { get; set; } = "dgoc2bids";
    public bool IsDebug { get; set; } = false;

    int PHandleMAX = 13;
    int BHandleMAX = 8;

    int? ATCPnlInd = null;
    double MultiplNum = 0;
    bool isPBExc = false;
    bool isInv = false;

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
            if (saa.Length >= 1)
            {
              switch (saa[0])
              {
                case "a":
                  if (saa.Length > 1) ATCPnlInd = int.Parse(saa[1]);
                  break;
                case "m":
                  if (saa.Length > 1) MultiplNum = double.Parse(saa[1]);
                  break;
                case "n":
                  if (saa.Length > 1) Name = saa[1];
                  break;
                case "pbexc":
                  isPBExc = true;
                  break;
                case "inv":
                  isInv = true;
                  break;
              }
            }
          }
          catch (Exception e) { Console.WriteLine("dgo2bids Connect() : {0}", e); }
        }
      }

      
      usbcom.Connect();
      usbcom.DataGot += Usbcom_DataGot;

      switch (usbcom.DevType)
      {
        case usbcom.DeviceNameList.shinkansen:
          PHandleMAX = 13;
          BHandleMAX = 8;
          break;
        case usbcom.DeviceNameList.type2:
          PHandleMAX = 5;
          BHandleMAX = 9;
          break;
        case usbcom.DeviceNameList.ryojo:
          //PHandleMAX = 13;
          //BHandleMAX = 8;
          break;
        case usbcom.DeviceNameList.mtc_p5b8:
          PHandleMAX = 5;
          BHandleMAX = 8;
          break;
        case usbcom.DeviceNameList.mtc_p5b6:
          PHandleMAX = 5;
          BHandleMAX = 6;
          break;
      }
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

      int? BNum = BNumGet(e.Data, usbcom.DevType);
      if (BNum != null) 
      {
        int b = (int)BNum;
        if (isInv) b = BHandleMAX - b;
        if (isPBExc) Common.PowerNotchNum = b;
        else Common.BrakeNotchNum = b >= BHandleMAX ? 255 : b;
      }

      int? PNum = PNumGet(e.Data, usbcom.DevType);
      if (PNum != null)
      {
        int p = (int)PNum;
        if (isInv) p = PHandleMAX - p;
        if (isPBExc) Common.BrakeNotchNum = p >= PHandleMAX ? 255 : p;
        else Common.PowerNotchNum = p;
      }

      bool[] keyState = Common.Ctrl_Key;
      CtrlerKeys cKeys = KeyStateGet(e.Data, usbcom.DevType);

      keyState[0] = cKeys.Horn | cKeys.A;//Horn SW
      keyState[1] = cKeys.APlus;
      keyState[4] = cKeys.S;//S Space
      keyState[5] = cKeys.Start;//A1 Ins
      keyState[6] = cKeys.Select;//A2 Del
      keyState[7] = cKeys.B;//B1 Home
      keyState[8] = cKeys.C;//B2 End
      keyState[9] = cKeys.Right;//C1 PUp
      keyState[10] = cKeys.Left;//C2 PDwn
      keyState[11] = cKeys.D;//D D2


      byte buf = e.Data[4];
      keyState[8] = (buf & 1) == 1;
      keyState[7] = ((buf >>= 1) & 1) == 1;
      keyState[6] = ((buf >>= 1) & 1) == 1;
      keyState[5] = ((buf >>= 1) & 1) == 1;
      keyState[9] = ((buf >>= 1) & 1) == 1;
      keyState[4] = ((buf >>= 1) & 1) == 1;

      int RNum = Common.ReverserNum;
      int rrec = RNum;
      if (cKeys.Up & cKeys.Down)
        RNum = 0;
      else if(cKeys.Up)
        RNum = RNum == 1 ? 0 : 1;
      else if(cKeys.Down)
        RNum = RNum == -1 ? 0 : -1;

      RNum = RNumGet(e.Data, usbcom.DevType) ?? RNum;

      if (RNum != rrec)
        Common.ReverserNum = RNum;

      Common.Ctrl_Key = keyState;
    }

    private int? PNumGet(in byte[] ba, usbcom.DeviceNameList devType)
    {
      switch (devType)
      {
        case usbcom.DeviceNameList.shinkansen:
          if (ba[1] == 0xff) return null;
          else return (int)Math.Round(ba[1] / 18.0, MidpointRounding.AwayFromZero) - 1;
        case usbcom.DeviceNameList.type2:
          PHandleMAX = 5;
          BHandleMAX = 9;
          break;
        case usbcom.DeviceNameList.ryojo:
          //PHandleMAX = 13;
          //BHandleMAX = 8;
          break;
        case usbcom.DeviceNameList.mtc_p5b8:
          PHandleMAX = 5;
          BHandleMAX = 8;
          break;
        case usbcom.DeviceNameList.mtc_p5b6:
          PHandleMAX = 5;
          BHandleMAX = 6;
          break;
      }
      return 0;
    }
    private int? BNumGet(in byte[] ba, usbcom.DeviceNameList devType)
    {
      switch (devType)
      {
        case usbcom.DeviceNameList.shinkansen:
          if (ba[0] == 0xff) return null;
          else return (int)Math.Round(ba[0] / 28.0, MidpointRounding.AwayFromZero) - 1;
        case usbcom.DeviceNameList.type2:
          PHandleMAX = 5;
          BHandleMAX = 9;
          break;
        case usbcom.DeviceNameList.ryojo:
          //PHandleMAX = 13;
          //BHandleMAX = 8;
          break;
        case usbcom.DeviceNameList.mtc_p5b8:
          PHandleMAX = 5;
          BHandleMAX = 8;
          break;
        case usbcom.DeviceNameList.mtc_p5b6:
          PHandleMAX = 5;
          BHandleMAX = 6;
          break;
      }
      return 99;
    }
    private int? RNumGet(in byte[] ba, usbcom.DeviceNameList devType)
    {
      return null;
    }
    private CtrlerKeys KeyStateGet(in byte[] ba,usbcom.DeviceNameList devType)
    {
      switch (devType)
      {
        case usbcom.DeviceNameList.shinkansen:
          return DGoKeyStateGet(ba);
        case usbcom.DeviceNameList.type2:
          return DGoKeyStateGet(ba);
        case usbcom.DeviceNameList.ryojo:
          return DGoKeyStateGet(ba);
        case usbcom.DeviceNameList.mtc_p5b8:
          return MTCKeyStateGet(ba);
        case usbcom.DeviceNameList.mtc_p5b6:
          return MTCKeyStateGet(ba);
      }
      return default;
    }
    private CtrlerKeys MTCKeyStateGet(in byte[] ba)
    {
      CtrlerKeys cKeys = new CtrlerKeys();
      return cKeys;
    }
    private CtrlerKeys DGoKeyStateGet(in byte[] ba)
    {
      CtrlerKeys cKeys = new CtrlerKeys();

      return cKeys;
    }

    public struct CtrlerKeys
    {
      public bool Start;
      public bool Select;
      public bool S;
      public bool A;
      public bool APlus;
      public bool B;
      public bool C;
      public bool D;
      public bool Up;
      public bool Right;
      public bool Down;
      public bool Left;
      public bool Horn;
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
    bool isBIDSEnabled = false;

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

      if (isBIDSEnabled)
      {
        byte buf = 0;
        if (ATCPnlInd == null)
        {
          ba[7] = 0xaa;
          ba[6] = 0xaa;
        }
        else
        {
          ba[7] = (byte)Math.Floor((ATCSPD % 1000) / 100.0);
          buf = (byte)Math.Floor((ATCSPD % 100) / 10.0);
          buf <<= 4;
          buf += (byte)(ATCSPD % 10);
          ba[6] = buf;
        }

        ba[5] = (byte)Math.Floor((CurrentSPD % 1000) / 100.0);
        buf = (byte)Math.Floor((CurrentSPD % 100) / 10.0);
        buf <<= 4;
        buf += (byte)(CurrentSPD % 10);
        ba[4] = buf;
      }
      else for (int i = 0; i < 8; i++) ba[i] = 0xaa;//Display Reset


      if (IsDebug)
      {
        string s = string.Empty;
        for (int i = 0; i < ba.Length; i++) s += ba[i].ToString() + " ";
        Console.WriteLine("{0} << {1}", Name, s);
      }
      usbcom.SendCtrl(ba, true);
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
        " -pbexc : (Power / Brake Exchange) Exchange roles of Power Handle and Brake Handle"+
        " -inv : (inverse) Inverse the Handle Position Counting");
    }
  }
}
