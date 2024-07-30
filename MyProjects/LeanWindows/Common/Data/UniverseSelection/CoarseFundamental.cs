/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Linq;
using System.Globalization;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// Defines summary information about a single symbol for a given date
    /// </summary>
    public class CoarseFundamental : BaseData
    {
        /// <summary>
        /// Gets the market for this symbol
        /// </summary>
        public string Market => Symbol.ID.Market;

        /// <summary>
        /// Gets the day's dollar volume for this symbol
        /// </summary>
        public virtual double DollarVolume { get; }

        /// <summary>
        /// Gets the day's total volume
        /// </summary>
        public virtual long Volume { get; }

        /// <summary>
        /// Returns whether the symbol has fundamental data for the given date
        /// </summary>
        public virtual bool HasFundamentalData { get; }

        /// <summary>
        /// Gets the price factor for the given date
        /// </summary>
        public virtual decimal PriceFactor { get; } = 1;

        /// <summary>
        /// Gets the split factor for the given date
        /// </summary>
        public virtual decimal SplitFactor { get; } = 1;

        /// <summary>
        /// Gets the combined factor used to create adjusted prices from raw prices
        /// </summary>
        public decimal PriceScaleFactor => PriceFactor * SplitFactor;

        /// <summary>
        /// Gets the split and dividend adjusted price
        /// </summary>
        public decimal AdjustedPrice => Price * PriceScaleFactor;

        /// <summary>
        /// The end time of this data.
        /// </summary>
        public override DateTime EndTime
        {
            get { return Time + QuantConnect.Time.OneDay; }
            set { Time = value - QuantConnect.Time.OneDay; }
        }
        
        /// <summary>
        /// Gets the raw price
        /// </summary>
        public override decimal Price => Value; 

        /// <summary>
        /// Initializes a new instance of the <see cref="CoarseFundamental"/> class
        /// </summary>
        public CoarseFundamental()
        {
        }

        /// <summary>
        /// Return the URL string source of the file. This will be converted to a stream
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source file</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>String URL of source file.</returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            throw new InvalidOperationException($"Coarse type is obsolete, please use {nameof(Fundamental)}");
        }

        /// <summary>
        /// Reader converts each line of the data source into BaseData objects. Each data type creates its own factory method, and returns a new instance of the object
        /// each time it is called.
        /// </summary>
        /// <param name="config">Subscription data config setup object</param>
        /// <param name="line">Line of the source document</param>
        /// <param name="date">Date of the requested data</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>Instance of the T:BaseData object generated by this line of the CSV</returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            throw new InvalidOperationException($"Coarse type is obsolete, please use {nameof(Fundamental)}");
        }

        /// <summary>
        /// Converts a given fundamental data point into row format
        /// </summary>
        public static string ToRow(CoarseFundamental coarse)
        {
            // sid,symbol,close,volume,dollar volume,has fundamental data,price factor,split factor
            var values = new object[]
            {
                coarse.Symbol.ID,
                coarse.Symbol.Value,
                coarse.Value,
                coarse.Volume,
                coarse.DollarVolume,
                coarse.HasFundamentalData,
                coarse.PriceFactor,
                coarse.SplitFactor
            };

            return string.Join(",", values.Select(s => Convert.ToString(s, CultureInfo.InvariantCulture)));
        }
    }
}