using Panoptes.Model.Charting;
using QuantConnect;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Panoptes.Model
{
    // For many elements we use custom objects in this tool.
    public static class ResultMapper
    {
        public static Dictionary<string, ChartDefinition> MapToChartDefinitionDictionary(this IDictionary<string, Chart> sourceDictionary)
        {
            return sourceDictionary == null ?
                new Dictionary<string, ChartDefinition>() :
                sourceDictionary.ToDictionary(entry => entry.Key, entry => MapToChartDefinition(entry.Value));
        }

        public static Dictionary<string, Chart> MapToChartDictionary(this IDictionary<string, ChartDefinition> sourceDictionary)
        {
            return sourceDictionary.ToDictionary(entry => entry.Key, entry => MapToChart(entry.Value));
        }

        private static InstantChartPoint MapToTimeStampChartPoint(this ChartPoint point)
        {
            return new InstantChartPoint
            {
                X = DateTimeOffset.FromUnixTimeSeconds(point.x), //Instant.FromUnixTimeSeconds(point.x),
                Y = (decimal)point.y
            };
        }
        private static InstantChartPoint MapToTimeStampChartPoint(this Candlestick point)
        {
            return new InstantChartPoint
            {
                X = point.Time, //Instant.FromUnixTimeSeconds(point.x),
                Y = (decimal)point.Low
            };
        }

        private static ChartPoint MapToChartPoint(this InstantChartPoint point)
        {
            return new ChartPoint
            {
                // QuantConnect chartpoints are always in Unix TimeStamp (seconds)
                x = point.X.ToUnixTimeSeconds(),
                y = point.Y
            };
        }

        private static ChartDefinition MapToChartDefinition(this Chart sourceChart)
        {
            return new ChartDefinition
            {
                Name = sourceChart.Name,
                Series = sourceChart.Series.MapToSeriesDefinitionDictionary()
            };
        }

        private static Chart MapToChart(this ChartDefinition sourceChart)
        {
            return new Chart
            {
                Name = sourceChart.Name,
                Series = sourceChart.Series.MapToSeriesDictionary()
            };
        }

        private static Dictionary<string, SeriesDefinition> MapToSeriesDefinitionDictionary(this IDictionary<string, BaseSeries> sourceSeries)
        {
            try
            {
                return sourceSeries.ToDictionary(entry => entry.Key, entry => ((Series)entry.Value).MapToSeriesDefinition());
            }
            catch
            {
                return sourceSeries.ToDictionary(entry => entry.Key, entry => entry.Value.MapToSeriesDefinition());
            }
        }

        private static Dictionary<string, BaseSeries> MapToSeriesDictionary(this IDictionary<string, SeriesDefinition> sourceSeries)
        {
            return sourceSeries.ToDictionary(entry => entry.Key, entry => entry.Value.MapToSeries());
        }

        private static SeriesDefinition MapToSeriesDefinition(this Series sourceSeries)
        {
            return new SeriesDefinition
            {
                Color = sourceSeries.Color,
                Index = sourceSeries.Index,
                Name = sourceSeries.Name,
                ScatterMarkerSymbol = sourceSeries.ScatterMarkerSymbol,
                SeriesType = sourceSeries.SeriesType,
                Unit = sourceSeries.Unit,
                Values = sourceSeries.Values.ConvertAll(v => ((ChartPoint)v).MapToTimeStampChartPoint())
            };
        }

        private static SeriesDefinition MapToSeriesDefinition(this BaseSeries sourceSeries)
        {
            switch (sourceSeries.SeriesType)
            {
                case SeriesType.Candle:
                    return new SeriesDefinition
                    {
                        //Color = ((Series)sourceSeries).Color,
                        Index = sourceSeries.Index,
                        Name = sourceSeries.Name,
                        //ScatterMarkerSymbol = ((Series)sourceSeries).ScatterMarkerSymbol,
                        SeriesType = sourceSeries.SeriesType,
                        Unit = sourceSeries.Unit,
                        Values = sourceSeries.Values.ConvertAll(v => ((Candlestick)v).MapToTimeStampChartPoint())
                    };
                default:
                    return new SeriesDefinition
                    {
                        Color = ((Series)sourceSeries).Color,
                        Index = sourceSeries.Index,
                        Name = sourceSeries.Name,
                        ScatterMarkerSymbol = ((Series)sourceSeries).ScatterMarkerSymbol,
                        SeriesType = sourceSeries.SeriesType,
                        Unit = sourceSeries.Unit,
                        Values = sourceSeries.Values.ConvertAll(v => ((ChartPoint)v).MapToTimeStampChartPoint())
                    };
            }
        }

        private static BaseSeries MapToSeries(this SeriesDefinition sourceSeries)
        {
            return new Series
            {
                Color = sourceSeries.Color,
                Index = sourceSeries.Index,
                Name = sourceSeries.Name,
                ScatterMarkerSymbol = sourceSeries.ScatterMarkerSymbol,
                SeriesType = sourceSeries.SeriesType,
                Unit = sourceSeries.Unit,
                Values = sourceSeries.Values.ConvertAll(v => (ISeriesPoint)v.MapToChartPoint())
            };
        }
    }
}
