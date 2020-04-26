using System;
using TR.BIDSSMemLib;
using TR.BIDSsv;

namespace TR.BIDSsvMOD.dgoCtrlUSB
{
  public class Main : IBIDSsv
  {
    public int Version => 102;
    public string Name { get; set; } = "dgoc2bids";
    public bool IsDebug { get; set; } = false;

    int PHandleMAX = 13;
    int CarBMax = 9;
    int CarPMax = 5;
    int BHandleMAX = 8;

    int? ATCPnlInd = null;
    double MultiplNum = 0;
    bool isPBExc = false;
    bool isInv = false;
    bool PCap = false;
    int?[] BTIndex = new int?[13];
    public bool Connect(in string args)
    {
      string[] sa = args.Replace(" ", string.Empty).ToLower().Split(new string[2] { "-", "/" }, StringSplitOptions.RemoveEmptyEntries);
      if (sa.Length > 1)
      {
        for (int i = 0; i < BTIndex.Length; i++) BTIndex[i] = null;
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
                case "pcap":
                  PCap = true;
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
                        BTIndex[(int)CtrlerKeysEN.Start] = btNAss;
                        break;
                      case "select":
                        BTIndex[(int)CtrlerKeysEN.Select] = btNAss;
                        break;
                      case "s":
                        BTIndex[(int)CtrlerKeysEN.S] = btNAss;
                        break;
                      case "a":
                        BTIndex[(int)CtrlerKeysEN.A] = btNAss;
                        break;
                      case "aplus":
                        BTIndex[(int)CtrlerKeysEN.APlus] = btNAss;
                        break;
                      case "b":
                        BTIndex[(int)CtrlerKeysEN.B] = btNAss;
                        break;
                      case "c":
                        BTIndex[(int)CtrlerKeysEN.C] = btNAss;
                        break;
                      case "d":
                        BTIndex[(int)CtrlerKeysEN.D] = btNAss;
                        break;
                      case "up":
                        BTIndex[(int)CtrlerKeysEN.Up] = btNAss;
                        break;
                      case "right":
                        BTIndex[(int)CtrlerKeysEN.Right] = btNAss;
                        break;
                      case "down":
                        BTIndex[(int)CtrlerKeysEN.Down] = btNAss;
                        break;
                      case "left":
                        BTIndex[(int)CtrlerKeysEN.Left] = btNAss;
                        break;
                      case "horn":
                        BTIndex[(int)CtrlerKeysEN.Horn] = btNAss;
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

      
      Usbcom.Connect();
      Usbcom.DataGot += Usbcom_DataGot;

      switch (Usbcom.DevType)
      {
        case Usbcom.DeviceNameList.shinkansen:
          PHandleMAX = 13;
          BHandleMAX = 8;
          break;
        case Usbcom.DeviceNameList.type2:
          PHandleMAX = 5;
          BHandleMAX = 9;
          break;
        case Usbcom.DeviceNameList.ryojo:
          //PHandleMAX = 13;
          //BHandleMAX = 8;
          break;
        case Usbcom.DeviceNameList.mtc_p5b8:
          PHandleMAX = 5;
          BHandleMAX = 8;
          break;
        case Usbcom.DeviceNameList.mtc_p5b6:
          PHandleMAX = 5;
          BHandleMAX = 6;
          break;
      }
      return true;
    }

    private void Usbcom_DataGot(object sender, Usbcom.DataGotEvArgs e)
    {
      if (IsDebug || PCap)
      {
        string s = string.Empty;
        for (int i = 0; i < e.Data.Length; i++) s += e.Data[i].ToString() + " ";
        Console.WriteLine("{0} << {1}", Name, s);
        if (PCap) return;
      }

      int? BNum = BNumGet(e.Data, Usbcom.DevType, CarBMax);
      if (BNum != null) 
      {
        int b = (int)BNum;
        if(IsDebug) Console.WriteLine("Brake : {0}", b);
        if (isInv) b = BHandleMAX - b;
        if (isPBExc) Common.PowerNotchNum = b;
        else Common.BrakeNotchNum = b >= BHandleMAX ? 255 : b;
      }

      int? PNum = PNumGet(e.Data, Usbcom.DevType);
      if (PNum != null)
      {
        int p = (int)PNum;
        if(IsDebug) Console.WriteLine("Power : {0}", p);
        if (isInv) p = PHandleMAX - p;
        if (isPBExc) Common.BrakeNotchNum = p >= PHandleMAX ? 255 : p;
        else Common.PowerNotchNum = p;
      }

      bool[] keyState = Common.Ctrl_Key;
      CtrlerKeys cKeys = KeyStateGet(e.Data, Usbcom.DevType);

      /*
      keyState[0] = cKeys.Horn | cKeys.C;//Horn SW
      keyState[1] = cKeys.APlus;
      keyState[4] = cKeys.S;//S Space
      keyState[5] = cKeys.Start;//A1 Ins
      keyState[6] = cKeys.Select;//A2 Del
      keyState[7] = cKeys.B;//B1 Home
      //keyState[8] = cKeys.C;//B2 End
      keyState[9] = cKeys.Right;//C1 PUp
      keyState[10] = cKeys.Left;//C2 PDwn
      keyState[11] = cKeys.D;//D D2
      */

      for(int i = 0; i < BTIndex.Length; i++)
      {
        if (BTIndex[i] == null) continue;
        int bti = BTIndex[i] ?? 0;
        switch ((CtrlerKeysEN)i)
        {
          case CtrlerKeysEN.A:
            keyState[bti] = cKeys.A;
            break;
          case CtrlerKeysEN.APlus:
            keyState[bti] = cKeys.APlus;
            break;
          case CtrlerKeysEN.B:
            keyState[bti] = cKeys.B;
            break;
          case CtrlerKeysEN.C:
            keyState[bti] = cKeys.C;
            break;
          case CtrlerKeysEN.D:
            keyState[bti] = cKeys.D;
            break;
          case CtrlerKeysEN.Down:
            keyState[bti] = cKeys.Down;
            break;
          case CtrlerKeysEN.Horn:
            keyState[bti] = cKeys.Horn;
            break;
          case CtrlerKeysEN.Left:
            keyState[bti] = cKeys.Left;
            break;
          case CtrlerKeysEN.Right:
            keyState[bti] = cKeys.Right;
            break;
          case CtrlerKeysEN.S:
            keyState[bti] = cKeys.S;
            break;
          case CtrlerKeysEN.Select:
            keyState[bti] = cKeys.Select;
            break;
          case CtrlerKeysEN.Start:
            keyState[bti] = cKeys.Start;
            break;
          case CtrlerKeysEN.Up:
            keyState[bti] = cKeys.Up;
            break;
        }
      }

      int RNum = Common.ReverserNum;
      int rrec = RNum;
      if (cKeys.Up & cKeys.Down)
        RNum = 0;
      else if(cKeys.Up)
        RNum = RNum == 1 ? 0 : 1;
      else if(cKeys.Down)
        RNum = RNum == -1 ? 0 : -1;

      RNum = RNumGet(e.Data, Usbcom.DevType) ?? RNum;

      if (RNum != rrec)
        Common.ReverserNum = RNum;

      Common.Ctrl_Key = keyState;
    }

    private int? PNumGet(in byte[] ba, Usbcom.DeviceNameList devType)
    {
      int hdp = ba[0] & 0b00001111;
      switch (devType)
      {
        case Usbcom.DeviceNameList.shinkansen:
          if (ba[1] == 0xff) return null;
          return (int)Math.Round(ba[1] / 18.0, MidpointRounding.AwayFromZero) - 1;
        case Usbcom.DeviceNameList.type2:
          if (ba[2] == 0xff) return null;
          switch (ba[2])
          {
            case 0x81: return 0;
            case 0x6D: return 1;
            case 0x54: return 2;
            case 0x3F: return 3;
            case 0x21: return 4;
            case 0x00: return 5;
            default: return null;
          }
        case Usbcom.DeviceNameList.ryojo:
          if (ba[1] == 0xff) return null;
          return ba[1] / 60;
        case Usbcom.DeviceNameList.ryojo_ub:
          if (ba[1] == 0xff) return null;
          return ba[1] / 60;
        case Usbcom.DeviceNameList.mtc_p5b8:
          if (hdp <= BHandleMAX) return 0;
          return hdp - BHandleMAX - 1;
        case Usbcom.DeviceNameList.mtc_p5b6:
          if (hdp <= BHandleMAX) return 0;
          return hdp - BHandleMAX - 1;
      }
      return 0;
    }
    private int? BNumGet(in byte[] ba, Usbcom.DeviceNameList devType, in int BMax = 9)
    {
      int hdp = ba[0] & 0b00001111;
      switch (devType)
      {
        case Usbcom.DeviceNameList.shinkansen:
          if (ba[0] == 0xff) return null;
          else return (int)Math.Round(ba[0] / 28.0, MidpointRounding.AwayFromZero) - 1;
        case Usbcom.DeviceNameList.type2:
          if (ba[1] == 0xff) return null;
          switch (ba[1])
          {
            case 0xB9: return 99;
            case 0xB5: return 8;
            case 0xB2: return 7;
            case 0xAF: return 6;
            case 0xA8: return 5;
            case 0xA2: return 4;
            case 0x9A: return 3;
            case 0x94: return 2;
            case 0x8A: return 1;
            case 0x79: return 0;
            default: return null;
          }
        case Usbcom.DeviceNameList.ryojo:
          if (ba[0] == 0xff) return null;
          return (int)Math.Round(((double)(ba[0] - 39) / 177) * BMax, MidpointRounding.AwayFromZero);
        case Usbcom.DeviceNameList.ryojo_ub:
          if (ba[0] == 0xff) return null;
          return (int)Math.Round(((double)(ba[0] - 39) / 177) * BMax, MidpointRounding.AwayFromZero);
        case Usbcom.DeviceNameList.mtc_p5b8:
          if (hdp > BHandleMAX) return 0;
          return BHandleMAX - hdp + 1;
        case Usbcom.DeviceNameList.mtc_p5b6:
          if (hdp > BHandleMAX) return 0;
          return BHandleMAX - hdp + 1;
      }
      return 99;
    }
    private int? RNumGet(in byte[] ba, Usbcom.DeviceNameList devType)
    {
      switch (devType)
      {
        case Usbcom.DeviceNameList.mtc_p5b8:
          if ((ba[0] & 0b10000000) != 0) return 1;
          if ((ba[0] & 0b01000000) != 0) return -1;
          return 0;
        case Usbcom.DeviceNameList.mtc_p4b8:
          if ((ba[0] & 0b10000000) != 0) return 1;
          if ((ba[0] & 0b01000000) != 0) return -1;
          return 0;
        case Usbcom.DeviceNameList.mtc_p5b6:
          if ((ba[0] & 0b00100000) != 0) return 1;
          if ((ba[0] & 0b00010000) != 0) return -1;
          return 0;
      }
      return null;
    }

    private CtrlerKeys KeyStateGet(in byte[] ba,Usbcom.DeviceNameList devType)
    {
      switch (devType)
      {
        case Usbcom.DeviceNameList.shinkansen:
          return DGoKeyStateGet(ba);
        case Usbcom.DeviceNameList.type2:
          return DGoKeyStateGet(ba);
        case Usbcom.DeviceNameList.ryojo:
          return DGoKeyStateGet(ba);
        case Usbcom.DeviceNameList.ryojo_ub:
          return DGoKeyStateGet(ba);
        case Usbcom.DeviceNameList.mtc_p5b8:
          return MTCKeyStateGet(ba);
        case Usbcom.DeviceNameList.mtc_p5b6:
          return MTCKeyStateGet(ba);
      }
      return default;
    }
    private CtrlerKeys MTCKeyStateGet(in byte[] ba)
    {
      CtrlerKeys cKeys = new CtrlerKeys();
      byte buf = ba[1];
      cKeys.S = (buf & 1) == 1;
      cKeys.D = ((buf >>= 1) & 1) == 1;
      cKeys.A = ((buf >>= 1) & 1) == 1;
      cKeys.APlus = ((buf >>= 1) & 1) == 1;
      cKeys.B = ((buf >>= 1) & 1) == 1;
      cKeys.C = ((buf >>= 1) & 1) == 1;

      buf = ba[2];
      cKeys.Start = (buf & 1) == 1;
      cKeys.Select = ((buf >>= 1) & 1) == 1;
      cKeys.Up = ((buf >>= 1) & 1) == 1;
      cKeys.Down = ((buf >>= 1) & 1) == 1;
      cKeys.Left = ((buf >>= 1) & 1) == 1;
      cKeys.Right = ((buf >>= 1) & 1) == 1;
      return cKeys;
    }
    private CtrlerKeys DGoKeyStateGet(in byte[] ba)
    {
      int bias = 0;
      switch (Usbcom.DevType)
      {
        case Usbcom.DeviceNameList.shinkansen:
          bias = 1;
          break;
        case Usbcom.DeviceNameList.ryojo:
          bias = 1;
          break;
        case Usbcom.DeviceNameList.ryojo_ub:
          bias = 1;
          break;
      }
      CtrlerKeys cKeys = new CtrlerKeys();
      byte buf = ba[5 - bias];
      cKeys.D = (buf & 1) == 1;
      cKeys.C = ((buf >>= 1) & 1) == 1;
      cKeys.B = ((buf >>= 1) & 1) == 1;
      cKeys.S = ((buf >>= 1) & 1) == 1;
      cKeys.Select = ((buf >>= 1) & 1) == 1;
      cKeys.Start = ((buf >>= 1) & 1) == 1;
      cKeys.Horn = ba[3 - bias] == 0;

      buf = ba[4 - bias];
      if (((buf >> 3) & 1) != 1)
      {
        switch (ba[4 - bias] & 0b00000111)
        {
          case 0b000:
            cKeys.Up = true;
            break;
          case 0b001:
            cKeys.Up = true;
            cKeys.Right = true;
            break;
          case 0b010:
            cKeys.Right = true;
            break;
          case 0b011:
            cKeys.Right = true;
            cKeys.Down = true;
            break;
          case 0b100:
            cKeys.Down = true;
            break;
          case 0b101:
            cKeys.Down = true;
            cKeys.Left = true;
            break;
          case 0b110:
            cKeys.Left = true;
            break;
          case 0b111:
            cKeys.Left = true;
            cKeys.Up = true;
            break;
        }
      }
      return cKeys;
    }

    public enum CtrlerKeysEN
    {
      Start,
      Select,
      S,
      A,
      APlus,
      B,
      C,
      D,
      Up,
      Right,
      Down,
      Left,
      Horn
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
      Usbcom.DataGot -= Usbcom_DataGot;
      Usbcom.Close();
      isBIDSEnabled = false;
      UpdateDisplay();
    }

    const double SPLEDSpdCount = 350 / 0x17;
    byte LEDBar = 0;
    short CurrentSPD = 0;
    short ATCSPD = 0;
    bool isDoorClosed = false;
    bool isBIDSEnabled = false;

    public void OnBSMDChanged(in BIDSSharedMemoryData data)
    {
      
      if (PCap) return;
      isBIDSEnabled = data.IsEnabled;
      if (data.SpecData.B != 0) CarBMax = data.SpecData.B;
      if (data.SpecData.P != 0) CarPMax = data.SpecData.P;
      if (doesHaveLamp(Usbcom.DevType))
      {
        LEDBar = (byte)(Math.Abs(data.StateData.V / SPLEDSpdCount));
        CurrentSPD = (short)(Math.Abs(data.StateData.V));

        isDoorClosed = data.IsDoorClosed;

        UpdateDisplay();
      }
    }

    private bool doesHaveLamp(Usbcom.DeviceNameList dn)
    {
      switch (dn)
      {
        case Usbcom.DeviceNameList.ryojo: return true;
        case Usbcom.DeviceNameList.ryojo_ub: return true;
        case Usbcom.DeviceNameList.shinkansen: return true;
        case Usbcom.DeviceNameList.type2: return true;
        default: return false;
      }
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
          ba[7] = (byte)((ATCSPD % 1000) / 100.0);
          buf = (byte)((ATCSPD % 100) / 10.0);
          buf <<= 4;
          buf += (byte)(ATCSPD % 10);
          ba[6] = buf;
        }

        ba[5] = (byte)((CurrentSPD % 1000) / 100.0);
        buf = (byte)((CurrentSPD % 100) / 10.0);
        buf <<= 4;
        buf += (byte)(CurrentSPD % 10);
        ba[4] = buf;
      }
      else for (int i = 0; i < 8; i++) ba[i] = (byte)(i < 4 ? 0x00 : 0xaa);//Display Reset


      if (IsDebug)
      {
        string s = string.Empty;
        for (int i = 0; i < ba.Length; i++) s += ba[i].ToString() + " ";
        Console.WriteLine("{0} << {1}", Name, s);
      }
      Usbcom.SendCtrl(ba, true);
    }

    public void OnOpenDChanged(in OpenD data) { }

    public void OnPanelDChanged(in int[] data) {
      if (PCap) return;
      if (doesHaveLamp(Usbcom.DevType) && ATCPnlInd != null && data?.Length > ATCPnlInd)
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
        " -btXXX : (brake) assign the work to button (default : null)(XXX:start, select etc)" +
        " -m : (magnification) set the magnification number that multiply with ATC Panel value" +
        " -n : (name) set the name of this connection"+
        " -pbexc : (Power / Brake Exchange) Exchange roles of Power Handle and Brake Handle"+
        " -pcap : Only do Packet Capture" +
        " -inv : (inverse) Inverse the Handle Position Counting");
    }
  }
}
