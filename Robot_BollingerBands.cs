using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScriptSolution;
using ScriptSolution.Indicators;
using ScriptSolution.Model;
//***************************************************************************
namespace Robot
{
    /// <summary>
    /// Робот на основе BollingerBands (2 части входа в сделку)
    /// </summary> 
    public class Robot_BollingerBands : Script
    {
//---------------------------------------------------------------------------
        /// <summary>
        /// Индиактор BollingerBands (вывод на основную панель)
        /// </summary>
        public CreateInidicator BB = new CreateInidicator(EnumIndicators.BollinderBands, 0, "BollinderBands");
        /// <summary>
        /// Назначение параметра первого тейк профита для Long
        /// </summary>
        public ParamOptimization TakeProfit_Long = new ParamOptimization(0.5, 0, 0, 0, "Тейк профит Long ");
        /// <summary>
        /// Назначение параметра первого тейк профита для Short
        /// </summary>
        public ParamOptimization TakeProfit_Short = new ParamOptimization(0.5, 0, 0, 0, "Тейк профит Short ");
//---------------------------------------------------------------------------
        public override void Execute()
        {
            // Пройти по всем свечам
            for (var bar = IndexBar; bar < CandleCount - 1; bar++)
            {
                // Пропуск до 100 баров
                if (bar < 100)
                    continue;
                //
                // Выполнить условия пробития BB
                {
                    // Если выше нижней полосы боллинджера
                    if (Candles.OpenSeries[bar] > BB.param.LinesIndicators[2].PriceSeries[bar])
                    {
                        TradeInfo.AccessLong = true;
                    }
                    // Если ниже верхней полосы боллинджера
                    if (Candles.OpenSeries[bar] < BB.param.LinesIndicators[1].PriceSeries[bar])
                    {
                        TradeInfo.AccessShort = true;
                    }
                }
                // Открытие позиции в Long
                if (TradeInfo.AccessLong == true)
                {
                    // Вход в Long позицию
                    if (Candles.CloseSeries[bar] < BB.param.LinesIndicators[2].PriceSeries[bar])
                    {
                        if (LongPos.Count == 0)
                        {
                            // Открыть полную позицию (2 части)
                            BuyAtClose(bar, 1, "Открытие Long_1");
                            BuyAtClose(bar, 1, "Открытие Long_2");
                            // закрыть до нового пересечения
                            TradeInfo.AccessLong = false;
                        }
                        if (LongPos.Count == 1)
                        {
                            BuyAtClose(bar, 1, "Дополнительная Long позиция");
                        }
                    }
                }
                // Открытие позиции в Short
                if (TradeInfo.AccessShort == true)
                {
                    // Вход в Long позицию
                    if (Candles.CloseSeries[bar] > BB.param.LinesIndicators[1].PriceSeries[bar])
                    {
                        if (ShortPos.Count == 0)
                        {
                            // Открыть полную позицию (2 части)
                            ShortAtClose(bar, 1, "Открытие Short_1");
                            ShortAtClose(bar, 1, "Открытие Short_2");
                            // закрыть до нового пересечения
                            TradeInfo.AccessShort = false;
                            // continue;
                        }
                        if (ShortPos.Count == 1)
                        {
                            ShortAtClose(bar, 1, "Дополнительная Short позиция");
                        }
                    }
                }
                //
                // Выход из позиции Long
                if (LongPos.Count > 0)
                {
                    // Полный выход из позиции
                    if (Candles.OpenSeries[bar+1] > BB.param.LinesIndicators[1].PriceSeries[bar+1])
                    {
                        // Закрыть все позиции
                        foreach (var Pos in LongPos)
                        {
                            SellAtClose(bar + 1, Pos, "Закрытие Long BB");
                            TradeInfo.SecondTradeBuy = false;
                        }
                    }
                    // Частичный возможный выход
                    if (LongPos.Count > 0 && TradeInfo.SecondTradeBuy == false)
                    {
                        for (int i = 0; i < LongPos.Count; i++)
                        {
                            if (Candles.OpenSeries[bar+1] > LongPos[i].EntryPrice + TakeProfit_Long.Value)
                            {
                                SellAtClose(bar + 1, LongPos[i], "Закрытие TakeProfit_Long");
                                TradeInfo.SecondTradeBuy = true;
                                break;
                            }
                        }
                    }
                }
                //
                // Выход из позиции Short
                // 
                if (ShortPos.Count > 0)
                {
                    // Полный выход из позиции
                    if (Candles.OpenSeries[bar+1] < BB.param.LinesIndicators[2].PriceSeries[bar+1])
                    {
                        // Закрыть все позиции
                        foreach (var Pos in ShortPos)
                        {
                            CoverAtClose(bar, Pos, "Закрытие Short BB");
                            TradeInfo.SecondTradeSell = false;
                        }
                    }
                    // Частичный возможный выход
                    if (ShortPos.Count > 0 && TradeInfo.SecondTradeSell == false)
                    {
                        for (int i = 0; i < ShortPos.Count; i++)
                        {
                            if (Candles.OpenSeries[bar+1] < ShortPos[i].EntryPrice - TakeProfit_Short.Value)
                            {
                                CoverAtClose(bar, ShortPos[i], "Закрытие TakeProfit_Short");
                                TradeInfo.SecondTradeSell = true;
                                break;
                            }
                        }
                    }
                }
            }
        }
//---------------------------------------------------------------------------        
        /// <summary>
        /// Получить аттрибуты по стратегии
        /// </summary>
        public override void GetAttributesStratetgy()
        {
            DesParamStratetgy.Version = "1.0";
            DesParamStratetgy.DateRelease = "28.05.2022";
            DesParamStratetgy.DateChange = "28.05.2022";
            DesParamStratetgy.Author = "Vybornov S.Y";
            DesParamStratetgy.Description = "Вход на покупку , если цена ниже нижней полосы BB, " +
                                            "Вход на продажу , если цена выше верхней полосы BB";
            DesParamStratetgy.Change = "";
            DesParamStratetgy.NameStrategy = "BB_LongShort";

        }
//---------------------------------------------------------------------------
    }
//-------------------------------------------------------------------------------------------------------------------
//-------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Класс информации о торговле
    /// </summary>
    public static class TradeInfo
    {
        /// <summary>
        /// Флаг дополнительного входа в Long 
        /// </summary>
        public static bool SecondTradeBuy = false;
        /// <summary>
        /// Флаг дополнительного входа в Short 
        /// </summary>
        public static bool SecondTradeSell = false;
        /// <summary>
        /// Разрешение торговли в Long
        /// </summary>
        public static bool AccessLong = false;
        /// <summary>
        /// Разрешение торговли в Short
        /// </summary>
        public static bool AccessShort = false;
    }
//---------------------------------------------------------------------------
}