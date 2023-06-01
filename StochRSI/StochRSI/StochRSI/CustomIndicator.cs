using IndicatorInterfaceCSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StochRSI
{
    public class StochRSI : IndicatorInterface
    {
        [Separator(Label = "Common")]
        public string Separator_Common;
        [Input(Name = "%K period")]
        public int InpStockKPeriod = 3;
        [Input(Name = "%D period")]
        public int InpStockDPeriod = 3;
        [Input(Name = "RSI Period")]
        public int InpRSIPeriod = 14;
        [Input(Name = "Stochastic Period")]
        public int InpStochastikPeriod = 14;
        [Input(Name = "RSI Applied Price")]
        public Applied_Price InpRSIAppliedPrice = Applied_Price.PRICE_CLOSE;

        public IndicatorBuffer _RSI = new IndicatorBuffer();
        public IndicatorBuffer AvGain = new IndicatorBuffer();
        public IndicatorBuffer AvLoss = new IndicatorBuffer();

        public IndicatorBuffer KBuffer = new IndicatorBuffer();
        public IndicatorBuffer DBuffer = new IndicatorBuffer();
        public IndicatorBuffer RSIBuffer = new IndicatorBuffer();
        public IndicatorBuffer StochBuffer = new IndicatorBuffer();

        const double EMPTY_VALUE = 0.00001;

        public override void OnInit()
        {
            SetIndicatorShortName("Stochastic-RSI");
            Indicator_Separate_Window = true;
            SetIndexBuffer(0, KBuffer);
            SetIndexStyle(0, DrawingStyle.DRAW_LINE, Color.DodgerBlue,LineStyle.STYLE_SOLID, 1);
            SetIndexLabel(0, "K Line");
            SetIndexBuffer(1, DBuffer);
            SetIndexStyle(1, DrawingStyle.DRAW_LINE, Color.OrangeRed, LineStyle.STYLE_SOLID, 1);
            SetIndexLabel(1, "D Line");
            SetLevel(20, Color.DarkGray, LineStyle.STYLE_DOT);
            SetLevel(80, Color.DarkGray, LineStyle.STYLE_DOT);
        }

        public override void OnCalculate(int index)
        {
            if (Bars() - index <= InpRSIPeriod || InpRSIPeriod == 0)
                return;

            if (Bars() - index == InpRSIPeriod + 1)
            {
                for (int i = index; i <= index + InpRSIPeriod - 1; i++)
                {
                    RSIBuffer[i] = GetRSI(InpRSIPeriod, InpRSIAppliedPrice, i);
                    StochBuffer[i] = Stoch(RSIBuffer, RSIBuffer, RSIBuffer, InpStochastikPeriod, i);
                    KBuffer[i] = Math.Max(MAOnArray(StochBuffer, InpStockKPeriod, i), EMPTY_VALUE);
                    DBuffer[i] = Math.Max(MAOnArray(KBuffer, InpStockDPeriod, i), EMPTY_VALUE);
                }
            } else
            {
                RSIBuffer[index] = GetRSI(InpRSIPeriod, InpRSIAppliedPrice, index);
                StochBuffer[index] = Stoch(RSIBuffer, RSIBuffer, RSIBuffer, InpStochastikPeriod, index);
                KBuffer[index] = Math.Max(MAOnArray(StochBuffer, InpStockKPeriod, index), EMPTY_VALUE);
                DBuffer[index] = Math.Max(MAOnArray(KBuffer, InpStockDPeriod, index), EMPTY_VALUE);
            }
        }

        public double Stoch(IndicatorBuffer source, IndicatorBuffer high, IndicatorBuffer low, int length, int shift)
        {
            double Highest = GetHighest(high, length, shift);
            double Lowest = GetLowest(low, length, shift);

            if (Highest - Lowest == 0)
            {
                return EMPTY_VALUE;
            }
            return 100 * (source[shift] - Lowest) / (Highest - Lowest);
        }

        public double GetLowest(IndicatorBuffer low, int length, int shift)
        {
            double Result = 0;
            for (int i = shift; i <= shift + length; i++)
            {
                if (Result == 0 || (low[i] < Result && low[i] != EMPTY_VALUE))
                {
                    Result = low[i];
                }
            }

            return Result;
        }

        public double GetHighest(IndicatorBuffer high, int length, int shift)
        {
            double Result = 0;
            for (int i = shift; i <= shift + length; i++)
            {
                if (Result == 0 || (high[i] > Result && high[i] != EMPTY_VALUE))
                {
                    Result = high[i];
                }
            }

            return Result;
        }

        double SimpleMA(int position,int period,IndicatorBuffer price, int rates_total)
        {
            double result = 0.0;

            if(position<=rates_total-period && period>0)
            {
                for(int i=0; i<period; i++)
                {
                    if(price[position + i]!=EMPTY_VALUE)
                    {
                        result+=price[position + i];
                    }
                }
                result /= period;
            }
            return (result);
        }
        public double MAOnArray(IndicatorBuffer Array, int Period, int index)
        {
            double sum = 0;
            double result = 0;

            for (int i = index; i < Period + index; i++)
            {
                sum = sum + Array[i];
                result = sum / Period;
            }

            return result;
        }
        public double GetRSI(int period, Applied_Price PriceType, int index)
        {
            double diff, gain, loss;

            if (Bars() - index <= period || period == 0)
                return 0;

            if (Bars() - index == period + 1)
            {
                gain = 0;
                loss = 0;
                for (int i = index; i <= index + period - 1; i++)
                {
                    diff = GetAppliedPrice(Symbol(), Period(), i, PriceType) - GetAppliedPrice(Symbol(), Period(), i + 1, PriceType);
                    if (diff > 0)
                        gain += diff;
                    else
                        loss -= diff;
                }
                AvGain[index] = gain / period;
                AvLoss[index] = loss / period;
            }
            else
            {
                gain = 0;
                loss = 0;
                diff = GetAppliedPrice(Symbol(), Period(), index, PriceType) - GetAppliedPrice(Symbol(), Period(), index + 1, PriceType);
                if (diff > 0)
                    gain = diff;
                else
                    loss = -diff;
                gain = (AvGain[index + 1] * (period - 1) + gain) / period;
                loss = (AvLoss[index + 1] * (period - 1) + loss) / period;
                AvGain[index] = gain;
                AvLoss[index] = loss;

                if (loss == 0)
                    _RSI[index] = 105;
                else
                    _RSI[index] = 100 - 100 / (1 + gain / loss);
            }

            return _RSI[index];
        }

    }

}
