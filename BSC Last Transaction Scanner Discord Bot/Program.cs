using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using System.IO;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using DSharpPlus.Entities;

namespace BSC_Last_Transaction_Scanner_Discord_Bot
{
    class Program
    {
        public static DiscordClient dc;
        public static Dictionary<string, Address> adresler;
        public static string[] bscscanapiKeys = { "Your https://bscscan.com/ API keys" };
        static void Main(string[] args)
        {
            Console.WriteLine("------------------------");
            adresler = new Dictionary<string, Address>();
            Start(args);
        }

        private static string GetMethod(string input)
        {
            if (input.Length >= 2500)
            {
                return "Contract Creation";
            }
            switch (input)
            {
                case "0x":
                    return "Transfer";
                case "0x095ea7b300000000000000000000000010ed43c718714eb63d5aa57b78b54704e256024effffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff":
                    return "Approve";
                default:
                    return "?";
            }
        }

        private static void Start(string[] args)
        {
            DiscordConfiguration dcConf = new DiscordConfiguration();
            dcConf.Token = "Your Discord Bot Token";
            dcConf.TokenType = TokenType.Bot;
            dc = new DiscordClient(dcConf);
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();

        }

        private static async Task MainAsync(string[] args)
        {
            dc.MessageCreated += Dc_MessageCreated;
            await dc.ConnectAsync();
            await Task.Delay(-1);
        }

        private static async Task Dc_MessageCreated(MessageCreateEventArgs e)
        {
            if (!e.Author.IsBot && e.Message.Content.StartsWith("-ekle"))
            {
                try
                {
                    string _address = e.Message.Content.Split(' ')[2];
                    string _isim = e.Message.Content.Split(' ')[1];
                    Address _a = new Address { address = _address, name = _isim, e = e };
                    adresler.Add(_isim, _a);
                    await e.Message.CreateReactionAsync(DiscordEmoji.FromName(dc, ":white_check_mark:"));
                    Thread th = new Thread(new ParameterizedThreadStart(GetLastTransactions));
                    th.Start(_a);
                }
                catch (Exception ex)
                {
                    await e.Message.RespondAsync(ex.Message);
                }



            }
            else if (!e.Author.IsBot && e.Message.Content.StartsWith("-sil"))
            {
                string _isim = e.Message.Content.Split(' ')[1];
                try
                {
                    if (adresler.ContainsKey(_isim))
                    {
                        adresler.Remove(_isim);
                        await e.Message.CreateReactionAsync(DiscordEmoji.FromName(dc, ":white_check_mark:"));
                    }
                    else
                    {
                        await e.Message.RespondAsync("Bulunamadığı için silinemedi!  :x:");
                    }
                }
                catch (Exception ex)
                {
                    await e.Message.RespondAsync(ex.Message);
                }
            }
            else if (!e.Author.IsBot && e.Message.Content.Trim() == "-adresler")
            {
                string addss = "";
                try
                {
                    foreach (var item in adresler)
                    {
                        addss += item.Value.name + " :  " + item.Value.address + "\n";
                    }
                    await e.Message.RespondAsync(addss);
                }
                catch
                {
                    await e.Message.RespondAsync("Adres bulunamadı!");

                }
            }
            else if (!e.Author.IsBot && e.Message.Content.Trim() == "-yardım")
            {
                await e.Message.RespondAsync("-ekle <İsim> <Adres>\n-sil <İsim>");
            }
        }


        public static int i = 0;
        static async void GetLastTransactions(object a)
        {
            string txhash = "";
            Address _ad = (Address)a;
            while (adresler.ContainsKey(_ad.name))
            {
                if (i > bscscanapiKeys.Length-1) { i = 0; }
                try
                {
                    var url = "https://api.bscscan.com/api?module=account&action=txlist&address=" + _ad.address + "&startblock=0&endblock=99999999&page=1&offset=1&sort=desc&apikey=" + bscscanapiKeys[i];
                    i++;
                    var jsonData = Get(url);
                    Response response = JsonConvert.DeserializeObject<Response>(jsonData);
                    if (txhash!="" && txhash != response.result[0].hash)
                    {
                        Console.WriteLine(_ad.name + " " + txhash);
                        await _ad.e.Message.RespondAsync("> "+(response.result[0].from.ToLower() == _ad.address.ToLower() ? ":orange_circle: OUT " : ":green_circle: IN ")+"("+ GetMethod(response.result[0].input) + ") "+ "**"+ _ad.name+ "** >  `" + _ad.address+ "`\n> TxHash: `"+ response.result[0].hash+"`");
                    }
                    txhash = response.result[0].hash;
                }
                catch { Console.WriteLine("Fail"); }
            }
        }

        public static string Get(string uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

    }
    class Address
    {
        public string address { get; set; }
        public string name { get; set; }
        public MessageCreateEventArgs e { get; set; }
    }
    public class Response
    {
        public List<Result> result { get; set; }
    }
    public class Result
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
    }
}
