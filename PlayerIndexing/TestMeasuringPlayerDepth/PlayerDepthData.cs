using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestMeasuringPlayerDepth
{
    class PlayerDepthData
    {
        #region Member Variables
    private const double MillimetersPerInch       = 0.0393700787;
    private static readonly double HorizontalTanA = Math.Tan(28.5 * Math.PI / 180);
    private static readonly double VerticalTanA   = Math.Abs(Math.Tan(21.5 * Math.PI / 180));
             
    private int _DepthSum;
    private int _DepthCount;
    private int _LoWidth;
    private int _HiWidth;
    private int _LoHeight;
    private int _HiHeight;
    #endregion Member Variables


    #region Constructor
    public PlayerDepthData(int playerId, double frameWidth, double frameHeight)
    {
        this.PlayerId     = playerId;
        this.FrameWidth   = frameWidth;
        this.FrameHeight  = frameHeight;
        this._LoWidth     = int.MaxValue;
        this._HiWidth     = int.MinValue;
        this._LoHeight    = int.MaxValue;
        this._HiHeight    = int.MinValue;
    }
    #endregion Constructor


    #region Methods
    public void UpdateData(int x, int y, int depth)
    {
        this._DepthCount++;
        this._DepthSum  += depth;
        this._LoWidth    = Math.Min(this._LoWidth, x);
        this._HiWidth    = Math.Max(this._HiWidth, x);
        this._LoHeight   = Math.Min(this._LoHeight, y);
        this._HiHeight   = Math.Max(this._HiHeight, y);
    }
    #endregion Methods


    #region Properties
    public int PlayerId { get; private set; }
    public double FrameWidth { get; private set; }
    public double FrameHeight { get; private set; }


    public double Depth
    {
        get { return this._DepthSum / (double) this._DepthCount; }
    }


    public int PixelWidth
    {
        get { return this._HiWidth - this._LoWidth; }
    }


    public int PixelHeight
    {
        get { return this._HiHeight - this._LoHeight; }
    }


    public double RealWidth
    {
        get
        {
            double opposite = this.Depth * HorizontalTanA;
            return this.PixelWidth * 2 * opposite / this.FrameWidth * MillimetersPerInch;
        }
    }

    public double RealHeight
    {
        get
        {
            double opposite = this.Depth * VerticalTanA;
            return this.PixelHeight * 2 * opposite / this.FrameHeight * MillimetersPerInch;
        }
    }

    #endregion Properties
}
    }

