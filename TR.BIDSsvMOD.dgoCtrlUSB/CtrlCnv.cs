using System;
using System.Collections.Generic;
using System.Text;
using TR.BIDSSMemLib;
using TR.BIDSsv;

namespace TR.BIDSsvMOD.dgoCtrlUSB
{
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

  public class CtrlCnv
  {
    internal string Name;
    internal bool IsDebug;
    internal int PHandleMAX = 13;
    internal int CarBMax = 9;
    internal int CarPMax = 5;
    internal int BHandleMAX = 8;

    internal int? ATCPnlInd = null;
    internal double MultiplNum = 0;
    internal bool isPBExc = false;
    internal bool isInv = false;
    internal bool PCap = false;
    internal int?[] BTIndex = new int?[13];

    public void DataGot(in byte[] ba, DeviceNameList devType)
    {
      if (IsDebug || PCap)
      {
        string s = string.Empty;
        for (int i = 0; i < ba.Length; i++) s += ba[i].ToString() + " ";
        Console.WriteLine("{0} << {1}", Name, s);
        if (PCap) return;
      }

      int? BNum = BNumGet(ba, devType, CarBMax);
      if (BNum != null)
      {
        int b = (int)BNum;
        if (IsDebug) Console.WriteLine("Brake : {0}", b);
        if (isInv) b = BHandleMAX - b;
        if (isPBExc) Common.PowerNotchNum = b;
        else Common.BrakeNotchNum = b >= BHandleMAX ? 255 : b;
      }

      int? PNum = PNumGet(ba, devType);
      if (PNum != null)
      {
        int p = (int)PNum;
        if (IsDebug) Console.WriteLine("Power : {0}", p);
        if (isInv) p = PHandleMAX - p;
        if (isPBExc) Common.BrakeNotchNum = p >= PHandleMAX ? 255 : p;
        else Common.PowerNotchNum = p;
      }

      bool[] keyState = Common.Ctrl_Key;
      CtrlerKeys cKeys = KeyStateGet(ba, devType);

      for (int i = 0; i < BTIndex.Length; i++)
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
      else if (cKeys.Up)
        RNum = RNum == 1 ? 0 : 1;
      else if (cKeys.Down)
        RNum = RNum == -1 ? 0 : -1;

      RNum = RNumGet(ba, devType) ?? RNum;

      if (RNum != rrec)
        Common.ReverserNum = RNum;

      Common.Ctrl_Key = keyState;
    }

    public int? PNumGet(in byte[] ba, DeviceNameList devType)
    {
      int hdp = ba[0] & 0b00001111;
      switch (devType)
      {
        case DeviceNameList.shinkansen:
          if (ba[1] == 0xff) return null;
          return (int)Math.Round(ba[1] / 18.0, MidpointRounding.AwayFromZero) - 1;
        case DeviceNameList.type2:
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
        case DeviceNameList.ryojo:
          if (ba[1] == 0xff) return null;
          return ba[1] / 60;
        case DeviceNameList.ryojo_ub:
          if (ba[1] == 0xff) return null;
          return ba[1] / 60;
        case DeviceNameList.mtc_p5b8:
          if (hdp <= BHandleMAX) return 0;
          return hdp - BHandleMAX - 1;
        case DeviceNameList.mtc_p5b6:
          if (hdp <= BHandleMAX) return 0;
          return hdp - BHandleMAX - 1;
      }
      return 0;
    }
    public int? BNumGet(in byte[] ba, DeviceNameList devType, in int BMax = 9)
    {
      int hdp = ba[0] & 0b00001111;
      switch (devType)
      {
        case DeviceNameList.shinkansen:
          if (ba[0] == 0xff) return null;
          else return (int)Math.Round(ba[0] / 28.0, MidpointRounding.AwayFromZero) - 1;
        case DeviceNameList.type2:
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
        case DeviceNameList.ryojo:
          if (ba[0] == 0xff) return null;
          return (int)Math.Round(((double)(ba[0] - 39) / 177) * BMax, MidpointRounding.AwayFromZero);
        case DeviceNameList.ryojo_ub:
          if (ba[0] == 0xff) return null;
          return (int)Math.Round(((double)(ba[0] - 39) / 177) * BMax, MidpointRounding.AwayFromZero);
        case DeviceNameList.mtc_p5b8:
          if (hdp > BHandleMAX) return 0;
          return BHandleMAX - hdp + 1;
        case DeviceNameList.mtc_p5b6:
          if (hdp > BHandleMAX) return 0;
          return BHandleMAX - hdp + 1;
      }
      return 99;
    }
    public int? RNumGet(in byte[] ba, DeviceNameList devType)
    {
      switch (devType)
      {
        case DeviceNameList.mtc_p5b8:
          if ((ba[0] & 0b10000000) != 0) return 1;
          if ((ba[0] & 0b01000000) != 0) return -1;
          return 0;
        case DeviceNameList.mtc_p4b8:
          if ((ba[0] & 0b10000000) != 0) return 1;
          if ((ba[0] & 0b01000000) != 0) return -1;
          return 0;
        case DeviceNameList.mtc_p5b6:
          if ((ba[0] & 0b00100000) != 0) return 1;
          if ((ba[0] & 0b00010000) != 0) return -1;
          return 0;
      }
      return null;
    }

    private CtrlerKeys KeyStateGet(in byte[] ba, DeviceNameList devType)
    {
      switch (devType)
      {
        case DeviceNameList.shinkansen:
          return DGoKeyStateGet(ba, devType);
        case DeviceNameList.type2:
          return DGoKeyStateGet(ba, devType);
        case DeviceNameList.ryojo:
          return DGoKeyStateGet(ba, devType);
        case DeviceNameList.ryojo_ub:
          return DGoKeyStateGet(ba, devType);
        case DeviceNameList.mtc_p5b8:
          return MTCKeyStateGet(ba, devType);
        case DeviceNameList.mtc_p5b6:
          return MTCKeyStateGet(ba, devType);
      }
      return default;
    }
    private CtrlerKeys MTCKeyStateGet(in byte[] ba, DeviceNameList devType)
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
    private CtrlerKeys DGoKeyStateGet(in byte[] ba, DeviceNameList devType)
    {
      int bias = 0;
      switch (devType)
      {
        case DeviceNameList.shinkansen:
          bias = 1;
          break;
        case DeviceNameList.ryojo:
          bias = 1;
          break;
        case DeviceNameList.ryojo_ub:
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

    public void Dispose() { }
    

    const double SPLEDSpdCount = 350 / 0x17;
    byte LEDBar = 0;
    short CurrentSPD = 0;
    short ATCSPD = 0;
    bool isDoorClosed = false;
    bool isBIDSEnabled = false;

    private bool doesHaveLamp(DeviceNameList dn)
    {
      switch (dn)
      {
        case DeviceNameList.ryojo: return true;
        case DeviceNameList.ryojo_ub: return true;
        case DeviceNameList.shinkansen: return true;
        case DeviceNameList.type2: return true;
        default: return false;
      }
    }

    public byte[] UpdateDisplayBAGet(DeviceNameList devType)
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

      return ba;
    }


    public byte[] OnBSMDChanged(in BIDSSharedMemoryData data, DeviceNameList devType)
    {

      if (PCap) return null;
      isBIDSEnabled = data.IsEnabled;
      if (data.SpecData.B != 0) CarBMax = data.SpecData.B;
      if (data.SpecData.P != 0) CarPMax = data.SpecData.P;
      if (doesHaveLamp(devType))
      {
        LEDBar = (byte)(Math.Abs(data.StateData.V / SPLEDSpdCount));
        CurrentSPD = (short)(Math.Abs(data.StateData.V));

        isDoorClosed = data.IsDoorClosed;
      }

      return UpdateDisplayBAGet(devType);
    }

    public byte[] OnPanelDChanged(in int[] data, DeviceNameList devType)
    {
      if (PCap) return null;
      if (doesHaveLamp(devType) && ATCPnlInd != null && data?.Length > ATCPnlInd)
      {
        short atcspdRec = ATCSPD;
        ATCSPD = (short)(data[ATCPnlInd ?? 0] * MultiplNum);
        return atcspdRec != ATCSPD ? UpdateDisplayBAGet(devType) : null;
      }
      return null;
    }

  }
}
