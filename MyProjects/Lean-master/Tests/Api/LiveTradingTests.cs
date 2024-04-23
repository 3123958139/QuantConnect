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
using System.IO;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Api;
using System.Threading;
using QuantConnect.Orders;
using QuantConnect.Brokerages;
using QuantConnect.Configuration;
using System.Collections.Generic;
using Python.Runtime;

namespace QuantConnect.Tests.API
{
    /// <summary>
    /// API Live endpoint tests
    /// </summary>
    [TestFixture, Explicit("Requires configured api access, a live node to run on, and brokerage configurations.")]
    public class LiveTradingTests : ApiTestBase
    {
        private const bool StopLiveAlgos = true;

        /// <summary>
        /// Live paper trading via Interactive Brokers
        /// </summary>
        [Test]
        public void LiveIBTest()
        {
            var user = Config.Get("ib-user-name");
            var password = Config.Get("ib-password");
            var account = Config.Get("ib-account");
            var environment = account.Substring(0, 2) == "DU" ? "paper" : "live";
            var ib_weekly_restart_utc_time = Config.Get("ib-weekly-restart-utc-time");

            // Create default algorithm settings
            var settings = new Dictionary<string, object>() {
                { "id", "InteractiveBrokersBrokerage" },
                { "environment", environment },
                { "ib-user-name", user },
                { "ib-password", password },
                { "ib-account", account },
                { "ib-weekly-restart-utc-time", ib_weekly_restart_utc_time},
                { "holdings", new List<Holding>() },
                { "cash", new List<Dictionary<object, object>>()
                    {
                    {new Dictionary<object, object>
                        {
                            { "currency" , "USD"},
                            { "amount", 100000}
                        }
                    }
                    }
                },
            };

            var quantConnectDataProvider = new Dictionary<string, object>
            {
                { "id", "QuantConnectBrokerage" },
            };

            var dataProviders = new Dictionary<string, object>
            {
                { "QuantConnectBrokerage", quantConnectDataProvider },
                { "InteractiveBrokersBrokerage", settings }
            };

            var file = new ProjectFile
            {
                Name = "Main.cs",
                Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs")
            };

            RunLiveAlgorithm(settings, file, StopLiveAlgos, dataProviders);
        }

        /// <summary>
        /// Live paper trading via FXCM
        /// </summary>
        [Test]
        public void LiveFXCMTest()
        {
            var user = Config.Get("fxcm-user-name");
            var password = Config.Get("fxcm-password");
            var account = Config.Get("fxcm-account-id");

            // Create default algorithm settings
            var settings = new Dictionary<string, object>() {
                { "id", "FxcmBrokerage" },
                { "environment", "paper" },
                { "user", user },
                { "password", password },
                { "account", account }
            };

            var file = new ProjectFile
            {
                Name = "Main.cs",
                Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateForexAlgorithm.cs")
            };

            RunLiveAlgorithm(settings, file, StopLiveAlgos);
        }

        /// <summary>
        /// Live paper trading via Oanda
        /// </summary>
        [Test]
        public void LiveOandaTest()
        {
            var token = Config.Get("oanda-access-token");
            var account = Config.Get("oanda-account-id");

            // Create default algorithm settings
            var settings = new Dictionary<string, object>()
            {
                { "id", "OandaBrokerage" },
                { "environment", "paper" },
                { "account", account },
                { "dateIssued", "1" },
                { "accessToken", token }
            };

            var file = new ProjectFile
            {
                Name = "Main.cs",
                Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateForexAlgorithm.cs")
            };

            RunLiveAlgorithm(settings, file, StopLiveAlgos);
        }

        /// <summary>
        /// Live paper trading via Tradier
        /// </summary>
        [Test]
        public void LiveTradierTest()
        {
            var refreshToken = Config.Get("tradier-refresh-token");
            var account = Config.Get("tradier-account-id");
            var accessToken = Config.Get("tradier-access-token");
            var dateIssued = Config.Get("tradier-issued-at");

            // Create default algorithm settings
            var settings = new Dictionary<string, object>()
            {
                { "id", "TradierBrokerage" },
                { "environment", "live" },
                { "accessToken", accessToken },
                { "dateIssued", dateIssued },
                { "refreshToken", refreshToken },
                { "lifetime", "86399"},
                { "account", account}
            };

            var file = new ProjectFile
            {
                Name = "Main.cs",
                Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs")
            };

            RunLiveAlgorithm(settings, file, StopLiveAlgos);
        }

        /// <summary>
        /// Live trading via Bitfinex
        /// </summary>
        [Test]
        public void LiveBitfinexTest()
        {
            var key = Config.Get("bitfinex-api-key");
            var secretKey = Config.Get("bitfinex-api-secret");

            // Create default algorithm settings
            var settings = new Dictionary<string, object>()
            {
                { "id", "BitfinexBrokerage" },
                { "environment", "live" },
                { "bitfinex-api-secret", secretKey },
                { "bitfinex-api-key", key }
            };

            var file = new ProjectFile
            {
                Name = "Main.cs",
                Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs")
            };

            RunLiveAlgorithm(settings, file, StopLiveAlgos);
        }

        /// <summary>
        /// Live trading via Coinbase
        /// </summary>
        [Test]
        public void LiveCoinbaseTest()
        {
            var key = Config.Get("coinbase-api-key");
            var secretKey = Config.Get("coinbase-api-secret");
            var apiUrl = Config.Get("coinbase-rest-api");
            var wsUrl = Config.Get("coinbase-url");

            // Create default algorithm settings
            var settings = new Dictionary<string, object>()
            {
                { "id", "CoinbaseBrokerage" },
                { "environment", "live" },
                { "key", key },
                { "secret", secretKey },
                { "apiUrl", apiUrl },
                { "wsUrl", wsUrl },
            };

            var file = new ProjectFile
            {
                Name = "Main.cs",
                Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs")
            };

            RunLiveAlgorithm(settings, file, StopLiveAlgos);
        }

        /// <summary>
        /// Test creating the settings object that provide the necessary parameters for each broker
        /// </summary>
        [Test]
        public void LiveAlgorithmSettings_CanBeCreated_Successfully()
        {
            var user = "";
            var password = "";
            var environment = "paper";
            var account = "";
            var key = "";
            var secretKey = "";

            // Oanda Custom Variables
            var accessToken = "";

            // Tradier Custom Variables
            var dateIssued = "";
            var refreshToken = "";

            // Create and test settings for each brokerage
            foreach (BrokerageName brokerageName in Enum.GetValues(typeof(BrokerageName)))
            {
                Dictionary<string, string> settings = null;

                switch (brokerageName)
                {
                    case BrokerageName.Default:
                        user = Config.Get("default-username");
                        password = Config.Get("default-password");
                        settings = new Dictionary<string, string>()
                        {
                            { "id", BrokerageName.Default.ToString() },
                            { "environment", environment },
                            { "user", user },
                            { "password", password },
                            { "account", account }
                        };

                        Assert.IsTrue(settings["id"] == BrokerageName.Default.ToString());
                        break;
                    case BrokerageName.FxcmBrokerage:
                        user = Config.Get("fxcm-user-name");
                        password = Config.Get("fxcm-password");
                        settings = new Dictionary<string, string>()
                        {
                            { "id", BrokerageName.FxcmBrokerage.ToString() },
                            { "environment", environment },
                            { "user", user },
                            { "password", password },
                            { "account", account }
                        };

                        Assert.IsTrue(settings["id"] == BrokerageName.FxcmBrokerage.ToString());
                        break;
                    case BrokerageName.InteractiveBrokersBrokerage:
                        user = Config.Get("ib-user-name");
                        password = Config.Get("ib-password");
                        account = Config.Get("ib-account");
                        settings = new Dictionary<string, string>()
                        {
                            { "id", BrokerageName.InteractiveBrokersBrokerage.ToString() },
                            { "environment", account.Substring(0, 2) == "DU" ? "paper" : "live" },
                            { "user", user },
                            { "password", password },
                            { "acount", account }
                        };

                        Assert.IsTrue(settings["id"] == BrokerageName.InteractiveBrokersBrokerage.ToString());
                        break;
                    case BrokerageName.OandaBrokerage:
                        accessToken = Config.Get("oanda-access-token");
                        account = Config.Get("oanda-account-id");

                        settings = new Dictionary<string, string>()
                        {
                            { "id", BrokerageName.OandaBrokerage.ToStringInvariant() },
                            { "user", "" },
                            { "password", "" },
                            { "environment", environment },
                            { "account", account },
                            { "accessToken", accessToken },
                            { "dateIssued", "1" }
                        };
                        Assert.IsTrue(settings["id"] == BrokerageName.OandaBrokerage.ToString());
                        break;
                    case BrokerageName.TradierBrokerage:
                        dateIssued = Config.Get("tradier-issued-at");
                        refreshToken = Config.Get("tradier-refresh-token");
                        account = Config.Get("tradier-account-id");

                        settings = new Dictionary<string, string>()
                        {
                            { "id", BrokerageName.TradierBrokerage.ToString() },
                            { "user", "" },
                            { "password", "" },
                            { "environment", "live" },
                            { "accessToken", accessToken },
                            { "dateIssued", dateIssued },
                            { "refreshToken", refreshToken },
                            { "lifetime", "86399" },
                            { "account", account }
                        };
                        break;
                    case BrokerageName.Bitfinex:
                        key = Config.Get("bitfinex-api-key");
                        secretKey = Config.Get("bitfinex-api-secret");

                        settings = new Dictionary<string, string>()
                        {
                            { "id", "BitfinexBrokerage" },
                            { "user", "" },
                            { "password", "" },
                            { "environment", "live" },
                            { "key", key },
                            { "secret", secretKey },
                        };
                        break;
                    case BrokerageName.GDAX:
                    case BrokerageName.Coinbase:
                        key = Config.Get("coinbase-api-key");
                        secretKey = Config.Get("coinbase-api-secret");
                        var apiUrl = Config.Get("coinbase-rest-api");
                        var wsUrl = Config.Get("coinbase-url");

                        settings = new Dictionary<string, string>()
                        {
                            { "id",  "CoinbaseBrokerage"},
                            { "user", "" },
                            { "password", "" },
                            { "environment", "live" },
                            { "key", key },
                            { "secret", secretKey },
                            { "apiUrl", (new Uri(apiUrl)).AbsoluteUri },
                            { "wsUrl", (new Uri(wsUrl)).AbsoluteUri }
                        };
                        break;
                    case BrokerageName.AlphaStreams:
                        // No live algorithm settings
                        settings = new Dictionary<string, string>()
                        {
                            { "id", "" },
                            { "user", "" },
                            { "password", "" },
                            { "account", "" }
                        };
                        break;
                    case BrokerageName.Binance:
                        // No live algorithm settings
                        settings = new Dictionary<string, string>()
                        {
                            { "id", "" },
                            { "user", "" },
                            { "password", "" },
                            { "account", "" }
                        };
                        break;
                    default:
                        throw new Exception($"Settings have not been implemented for this brokerage: {brokerageName}");
                }

                // Tests common to all brokerage configuration classes
                Assert.IsTrue(settings != null);
                Assert.IsTrue(settings["password"] == password);
                Assert.IsTrue(settings["user"] == user);

                // Oanda specific settings
                if (brokerageName == BrokerageName.OandaBrokerage)
                {
                    var oandaSetting = settings;
                    Assert.IsTrue(oandaSetting["accessToken"] == accessToken);
                }

                // Tradier specific settings
                if (brokerageName == BrokerageName.TradierBrokerage)
                {
                    var tradierLiveAlogrithmSettings = settings;

                    Assert.IsTrue(tradierLiveAlogrithmSettings["dateIssued"] == dateIssued);
                    Assert.IsTrue(tradierLiveAlogrithmSettings["refreshToken"] == refreshToken);
                    Assert.IsTrue(settings["environment"] == "life");
                }

                // reset variables
                user = "";
                password = "";
                environment = "paper";
                account = "";
            }
        }

        /// <summary>
        /// Reading live algorithm tests
        ///   - Get a list of live algorithms
        ///   - Get logs for the first algorithm returned
        /// Will there always be a live algorithm for the test user?
        /// </summary>
        [Test]
        public void LiveAlgorithmsAndLiveLogs_CanBeRead_Successfully()
        {
            // Read all currently running algorithms
            var liveAlgorithms = ApiClient.ListLiveAlgorithms(AlgorithmStatus.Running);

            Assert.IsTrue(liveAlgorithms.Success);
            // There has to be at least one running algorithm
            Assert.IsTrue(liveAlgorithms.Algorithms.Any());

            // Read the logs of the first live algorithm
            var firstLiveAlgo = liveAlgorithms.Algorithms[0];
            var liveLogs = ApiClient.ReadLiveLogs(firstLiveAlgo.ProjectId, firstLiveAlgo.DeployId);

            Assert.IsTrue(liveLogs.Success);
            Assert.IsTrue(liveLogs.Logs.Any());
        }

        /// <summary>
        /// Runs the algorithm with the given settings and file
        /// </summary>
        /// <param name="settings">Settings for Lean</param>
        /// <param name="file">File to run</param>
        /// <param name="stopLiveAlgos">If true the algorithm will be stopped at the end of the method.
        /// Otherwise, it will keep running</param>
        /// <param name="dataProviders">Dictionary with the data providers and their corresponding credentials</param>
        /// <returns>The id of the project created with the algorithm in</returns>
        private int RunLiveAlgorithm(Dictionary<string, object> settings, ProjectFile file, bool stopLiveAlgos, Dictionary<string, object> dataProviders = null)
        {
            // Create a new project
            var project = ApiClient.CreateProject($"Test project - {DateTime.Now.ToStringInvariant()}", Language.CSharp, TestOrganization);
            var projectId = project.Projects.First().ProjectId;

            // Update Project Files
            var updateProjectFileContent = ApiClient.UpdateProjectFileContent(projectId, "Main.cs", file.Code);
            Assert.IsTrue(updateProjectFileContent.Success);

            // Create compile
            var compile = ApiClient.CreateCompile(projectId);
            Assert.IsTrue(compile.Success);

            // Wait at max 30 seconds for project to compile
            var compileCheck = WaitForCompilerResponse(projectId, compile.CompileId, 30);
            Assert.IsTrue(compileCheck.Success);
            Assert.IsTrue(compileCheck.State == CompileState.BuildSuccess);

            // Get a live node to launch the algorithm on
            var nodesResponse = ApiClient.ReadProjectNodes(projectId);
            Assert.IsTrue(nodesResponse.Success);
            var freeNode = nodesResponse.Nodes.LiveNodes.Where(x => x.Busy == false);
            Assert.IsNotEmpty(freeNode, "No free Live Nodes found");

            // Create live default algorithm
            var createLiveAlgorithm = ApiClient.CreateLiveAlgorithm(projectId, compile.CompileId, freeNode.FirstOrDefault().Id, settings, dataProviders: dataProviders);
            Assert.IsTrue(createLiveAlgorithm.Success);

            if (stopLiveAlgos)
            {
                // Liquidate live algorithm; will also stop algorithm
                var liquidateLive = ApiClient.LiquidateLiveAlgorithm(projectId);
                Assert.IsTrue(liquidateLive.Success);

                // Delete the project
                var deleteProject = ApiClient.DeleteProject(projectId);
                Assert.IsTrue(deleteProject.Success);
            }

            return projectId;
        }

        [Test]
        public void ReadLiveOrders()
        {
            // Create default algorithm settings
            var settings = new Dictionary<string, object>()
            {
                { "id", "Default" },
                { "environment", "paper" },
                { "user", "" },
                { "password", "" },
                { "account", "" }
            };

            var file = new ProjectFile
            {
                Name = "Main.cs",
                Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs")
            };

            // Run the live algorithm
            var projectId = RunLiveAlgorithm(settings, file, false);

            // Wait to receive the orders
            var readLiveOrders = WaitForReadLiveOrdersResponse(projectId, 60 * 5);
            Assert.IsTrue(readLiveOrders.Any());
            Assert.AreEqual(Symbols.SPY, readLiveOrders.First().Symbol);

            // Liquidate live algorithm; will also stop algorithm
            var liquidateLive = ApiClient.LiquidateLiveAlgorithm(projectId);
            Assert.IsTrue(liquidateLive.Success);

            // Delete the project
            var deleteProject = ApiClient.DeleteProject(projectId);
            Assert.IsTrue(deleteProject.Success);
        }

        [Test]
        public void RunLiveAlgorithmsFromPython()
        {
            var file = new ProjectFile
            {
                Name = "Main.cs",
                Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs")
            };
            // Create a new project
            var project = ApiClient.CreateProject($"Test project - {DateTime.Now.ToStringInvariant()}", Language.CSharp, TestOrganization);
            var projectId = project.Projects.First().ProjectId;

            // Update Project Files
            var updateProjectFileContent = ApiClient.UpdateProjectFileContent(projectId, "Main.cs", file.Code);
            Assert.IsTrue(updateProjectFileContent.Success);

            // Create compile
            var compile = ApiClient.CreateCompile(projectId);
            Assert.IsTrue(compile.Success);

            // Wait at max 30 seconds for project to compile
            var compileCheck = WaitForCompilerResponse(projectId, compile.CompileId, 30);
            Assert.IsTrue(compileCheck.Success);
            Assert.IsTrue(compileCheck.State == CompileState.BuildSuccess);

            // Get a live node to launch the algorithm on
            var nodesResponse = ApiClient.ReadProjectNodes(projectId);
            Assert.IsTrue(nodesResponse.Success);
            var freeNode = nodesResponse.Nodes.LiveNodes.Where(x => x.Busy == false);
            Assert.IsNotEmpty(freeNode, "No free Live Nodes found");

            using (Py.GIL())
            {

                dynamic pythonCall = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def CreateLiveAlgorithmFromPython(apiClient, projectId, compileId, nodeId):
    user = Config.Get('ib-user-name')
    password = Config.Get('ib-password')
    account = Config.Get('ib-account')
    environment = 'paper'
    if account[:2] == 'DU':
        environment = 'live'
    ib_weekly_restart_utc_time = '22:00:00'
    settings = {'id':'InteractiveBrokersBrokerage', 'environment': environment, 'ib-user-name': user, 'ib-password': password, 'ib-account': account, 'ib-weekly-restart-utc-time': ib_weekly_restart_utc_time, 'holdings':[], 'cash': [{'currency' : 'USD', 'amount' : 100000}]}
    dataProviders = {'QuantConnectBrokerage':{'id':'QuantConnectBrokerage'}}
    apiClient.CreateLiveAlgorithm(projectId, compileId, nodeId, settings, dataProviders = dataProviders)
");
                var createLiveAlgorithmModule = pythonCall.GetAttr("CreateLiveAlgorithmFromPython");
                var createLiveAlgorithm = createLiveAlgorithmModule(ApiClient, projectId, compile.CompileId, freeNode.FirstOrDefault().Id);
                Assert.IsTrue(createLiveAlgorithm.Success);

                // Liquidate live algorithm; will also stop algorithm
                var liquidateLive = ApiClient.LiquidateLiveAlgorithm(projectId);
                Assert.IsTrue(liquidateLive.Success);

                // Delete the project
                var deleteProject = ApiClient.DeleteProject(projectId);
                Assert.IsTrue(deleteProject.Success);
            }
        }

        /// <summary>
        /// Wait for the compiler to respond to a specified compile request
        /// </summary>
        /// <param name="projectId">Id of the project</param>
        /// <param name="compileId">Id of the compilation of the project</param>
        /// <param name="seconds">Seconds to allow for compile time</param>
        /// <returns></returns>
        private Compile WaitForCompilerResponse(int projectId, string compileId, int seconds)
        {
            var compile = new Compile();
            var finish = DateTime.Now.AddSeconds(seconds);
            while (DateTime.Now < finish)
            {
                Thread.Sleep(1000);
                compile = ApiClient.ReadCompile(projectId, compileId);
                if (compile.State == CompileState.BuildSuccess) break;
            }
            return compile;
        }

        /// <summary>
        /// Wait to receive at least one order
        /// </summary>
        /// <param name="projectId">Id of the project</param>
        /// <param name="seconds">Seconds to allow for receive an order</param>
        /// <returns></returns>
        private List<ApiOrderResponse> WaitForReadLiveOrdersResponse(int projectId, int seconds)
        {
            var readLiveOrders = new List<ApiOrderResponse>();
            var finish = DateTime.UtcNow.AddSeconds(seconds);
            while (DateTime.UtcNow < finish && !readLiveOrders.Any())
            {
                Thread.Sleep(10000);
                readLiveOrders = ApiClient.ReadLiveOrders(projectId);
            }
            return readLiveOrders;
        }
    }
}
