using CommandLine;
using DARReferenceData.DatabaseHandlers.Blockchain;
using DARReferenceData.DatabaseHandlers.Blockchain.Utils;
using DARReferenceData.Models.Blockchain.Bitcoin;
using DARReferenceData.Models.Blockchain.Ethereum;
using DARReferenceData.ViewModels.Blockchain;
using Newtonsoft.Json;

namespace SDKTest;

class Program
{
    static void Main(string[] args)
    {
        // Parser.Default.ParseArguments<Options>(args)
        //     .WithParsed(options =>
        //     {
        //         TestBitcoin(options);
        //     });
        //
        // Parser.Default.ParseArguments<Options>(args)
        //     .WithParsed(options =>
        //     {
        //         TestEthereum(options);
        //     });
        
        var options = new Options
        {
            IP = "52.20.37.157:8545/5432",
            AssetIdentifiers = new string[] { "eth" },
            WalletAddress = new string[] { "0x645dB031d6Ac9f3a69566123Bf26878A4990ebef" },
            StartDate = "2015-06-20 00:00:00.000000",
            EndDate = "2023-06-21 00:00:00.000000",
            AsOfDate = "2023-06-21 00:00:00.000000",
            ClientId = "1aeecu0i4qm6jq7nmuss4p2uhl",
            BatchSize = 10,
            Blockchain = "Ethereum"
        };

        var options2 = new Options
        {
            IP =
                "svc-f25fc74f-3558-4bf0-87b7-9cba732d1e41-dml.aws-virginia-4.svc.singlestore.com;Port=3306;Database=refmaster_internal_DEV;Uid=<USER>;Pwd=<PWD>;convert zero datetime=True",
            AssetIdentifiers = new[] { "btc" },
            WalletAddress = new[] { "3Pva5fLkVKRRZKSKHk7YwfSxSgCAPE4tYc" },
            Blockchain = "Bitcoin",
            StartDate = "2015-06-20 00:00:00.000000",
            EndDate = "2023-06-21 00:00:00.000000",
            AsOfDate = "2023-06-21 00:00:00.000000",
            ClientId = "1aeecu0i4qm6jq7nmuss4p2uhl",
            BatchSize = 10
        };

        TestBitcoin(options2);
        //Console.WriteLine(VerifyTransaction("0x645dB031d6Ac9f3a69566123Bf26878A4990ebef").Result);
        //TestEthereum(options);
    }


    private static async void TestBitcoin(Options options)
    {
        Bitcoin bitcoin = new Bitcoin(options.IP);

        var txnResult = bitcoin.GetTransactions(options.AssetIdentifiers, options.WalletAddress, options.Blockchain,
            options.StartDate, options.EndDate, options.ClientId, options.BatchSize);
        // var flattenedTxnResult = bitcoin.GetTransactionsFlattened(options.AssetIdentifiers, options.WalletAddress,
        //     options.Blockchain,
        //     options.StartDate, options.EndDate, options.ClientId, options.BatchSize);
        // var posResult = bitcoin.GetPositions(options.AssetIdentifiers, options.WalletAddress, options.Blockchain,
        //     options.AsOfDate, options.ClientId, options.BatchSize);
        
        Console.WriteLine(await VerifyBtcTransaction("3Pva5fLkVKRRZKSKHk7YwfSxSgCAPE4tYc"));

    }

    private static async void TestEthereum(Options options)
    {
        Ethereum ethereum = new Ethereum(new EthConn
            { EthIp = options.IP, BqProject = "dar-dev-02.blockchain_ethereum.transactions_token_transfers" });

        var (txnResult, txnError) = await ethereum.GetTransactions(options.AssetIdentifiers, options.WalletAddress,
            options.Blockchain,
            options.StartDate, options.EndDate, options.ClientId, options.BatchSize);
        
        var (posnResult, getPositionsError)  = ethereum.GetPositions(options.AssetIdentifiers, options.WalletAddress, options.Blockchain,
            options.AsOfDate, options.ClientId, options.BatchSize);
        
        Console.WriteLine("Results: " + txnResult.Equals(MapEtherscanApiResponseToAccountTxnModel(await VerifyEthTransaction("0x645dB031d6Ac9f3a69566123Bf26878A4990ebef"))));
    }


    private static async Task<string> VerifyBtcTransaction(string walletAddress)
    {
        var apiUrl = $"https://chain.api.btc.com/v3/address/{walletAddress}";
        using (var httpClient = new HttpClient())
        {
            try
            {
                var response = await httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();
                    return responseData; // Return true if transaction is verified
                }
                
            }
            catch (Exception ex) { Console.WriteLine($"Error verifying transaction: {ex.Message}"); }
            return "";
        }
    }

    private static async Task<string> VerifyEthTransaction(string walletAddress)
    {
        const string ApiKey = "ZNCWUR7S9K939NIWRZNYT7WFCY1UMGBK4C";
        var apiUrl =
            $"https://api.etherscan.io/api?module=account&action=txlist&address={walletAddress}&apikey={ApiKey}";
        
        using (var httpClient = new HttpClient())
        {
           
            try
            {
                var response = await httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();
                    return responseData; 
                }
            }
            catch (Exception ex) { Console.WriteLine($"Error verifying transaction: {ex.Message}"); }

            return "";
        }
    }

    private static AccountTxnViewModel MapEtherscanApiResponseToAccountTxnModel(string response)
    {
        var txnResp = JsonConvert.DeserializeObject<List<EtherscanAPIResponse>>(response);
        
        return new AccountTxnViewModel
        {
            Summary = "",
            Transactions = txnResp.Select(txn => new AccountTxnModel
            {
                BlockNumber = txn.blockNumber,
                BlockHash = txn.blockHash,
                BlockIndex = txn.transactionIndex,
                Fee = txn.gas,
                ToAddress = txn.to,
                FromAddress = txn.from,
                TxnHash = txn.hash,
                BlockTimestamp = txn.timeStamp,
                TokenContractAddress = txn.contractAddress,
                Amount = txn.value
            }).ToList()
        };
    }


    private static UtxoTxnViewModel MapBtcApiResponseToUTXOTxnModel(string response)
    {
        return new UtxoTxnViewModel()
        {
            Summary = "",
            Transactions = new List<UtxoTxnModel>()
        };
    }
}
/*
 * Sample response from Etherscan API:
 * {"blockNumber":"3992197","timeStamp":"1499502082",
 * "hash":"0x0d4a3f043bc90bc6366f14c4fe9f892925eeb51175ad743998701dc721b218b7","nonce":"9982",
 * "blockHash":"0x79de46c47c74c42bf9a1ca06c3b14d6b9c0fa00a6b98dd54024e9240903e4eb2","transactionIndex":"90",
 * "from":"0x645db031d6ac9f3a69566123bf26878a4990ebef","to":"0xbc3033728986ca1fc58394b946baca8f39e7b447",
 * "value":"88054000000000000","gas":"23101","gasPrice":"21486870188","isError":"0","txreceipt_status":"",
 * "input":"0x","contractAddress":"","cumulativeGasUsed":"3463754","gasUsed":"21000","confirmations":"15799948",
 * "methodId":"0x","functionName":""}
 */


class Options
{
    [Option("ip", Required = true, HelpText = "IP address")]
    public string IP { get; set; }

    [Option("asset-identifiers", Required = true, HelpText = "Asset identifiers")]
    public string[] AssetIdentifiers { get; set; }

    [Option("wallet-address", Required = true, HelpText = "Wallet addresses")]
    public string[] WalletAddress { get; set; }

    [Option("blockchain", Required = true, HelpText = "Blockchain")]
    public string Blockchain { get; set; }

    [Option("start-date", Required = true, HelpText = "Start Date")]
    public string StartDate { get; set; }

    [Option("end-date", Required = true, HelpText = "End Date")]
    public string EndDate { get; set; }

    [Option("as-of-date", Required = true, HelpText = "As of Date")]
    public string AsOfDate { get; set; }

    [Option("caller-id", Required = true, HelpText = "Caller ID")]
    public string ClientId { get; set; }

    [Option("batch-size", Required = true, HelpText = "Batch Size")]
    public int BatchSize { get; set; }
}


class EtherscanAPIResponse
{
    public string blockNumber { get; set; }
    public string timeStamp { get; set; }
    public string hash { get; set; }
    public string nonce { get; set; }
    public string blockHash { get; set; }
    public string transactionIndex { get; set; }
    public string from { get; set; }
    public string to { get; set; }
    public string value { get; set; }
    public string gas { get; set; }
    public string gasPrice { get; set; }
    public string isError { get; set; }
    public string txreceipt_status { get; set; }
    public string input { get; set; }
    public string contractAddress { get; set; }
    public string cumulativeGasUsed { get; set; }
    public string gasUsed { get; set; }
    public string confirmations { get; set; }
    public string methodId { get; set; }
    public string functionName { get; set; }

}