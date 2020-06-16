using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using VkNet.Abstractions;
using VkNet.Model;
using VkNet.Model.RequestParams;
using VkNet.Utils;
using HtmlAgilityPack;
using System.Text;

namespace SimpleVkPuzzleBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CallbackController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        private readonly IVkApi _vkApi;

        static String puzzleAnswer = "answer";
        static Boolean puzzleFlag = false;

        public CallbackController(IVkApi vkApi, IConfiguration configuration)
        {
            _vkApi = vkApi;
            _configuration = configuration;
        }

        [HttpPost]
        public IActionResult Callback([FromBody] Updates updates)
        {
            switch (updates.Type)
            {
                case "confirmation":
                    {
                        return Ok(_configuration["Config:Confirmation"]);
                    }
                case "message_new":
                    {

                        var msg = Message.FromJson(new VkResponse(updates.Object));

                        String callbackMessageText = " ";
                        // Если включен режим загадок
                        if (puzzleFlag)
                        {
                            if (msg.Text.ToLower() == puzzleAnswer.ToLower())
                            {

                                puzzleFlag = false;
                                _vkApi.Messages.Send(new MessagesSendParams
                                {
                                    RandomId = new DateTime().Millisecond,
                                    PeerId = msg.PeerId.Value,
                                    Message = "Верно! Вы дали правильный ответ :)"
                                });
                            }
                            else if (msg.Text.ToLower() == "посмотреть ответ")
                            {
                                puzzleFlag = false;
                                _vkApi.Messages.Send(new MessagesSendParams
                                {
                                    RandomId = new DateTime().Millisecond,
                                    PeerId = msg.PeerId.Value,
                                    Message = "Ответ : " + puzzleAnswer
                                    + "\n----Режим игры выключен----"
                                    + "\n----Доступны все основные команды----"
                                    + "\n----Для новой игры наберите 'загадка'----"
                                });
                            }
                            else
                            {
                                _vkApi.Messages.Send(new MessagesSendParams
                                {
                                    RandomId = new DateTime().Millisecond,
                                    PeerId = msg.PeerId.Value,
                                    Message = "Вы дали неверный ответ. Попробуйте еще!"
                                });
                            }
                        }
                        else
                        {

                            switch (msg.Text.ToLower())
                            {
                                case "привет":
                                    {
                                        callbackMessageText = "И тебе приветик :)";
                                        break;
                                    }
                                case "загадка":
                                    {
                                        puzzleFlag = true;
                                        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                                        Encoding.GetEncoding("windows-1251");

                                        var url = "https://www.potehechas.ru/zagadki/shutochnye_5.shtml";
                                        var web = new HtmlWeb();
                                        var doc = web.Load(url);

                                        var puzzles = doc.DocumentNode.SelectNodes("//div[@class='text1']");


                                        var rand = new Random();
                                        var randNumber = rand.Next(puzzles.Count);

                                        var desiredPuzzle = puzzles[randNumber];

                                        HtmlNode testvar = desiredPuzzle.NextSibling.NextSibling;

                                        var desiredIndex = testvar.Attributes["onmousedown"].Value.IndexOf("'");
                                        puzzleAnswer = testvar.Attributes["onmousedown"].Value.Substring(desiredIndex);
                                        var puzzleAndswerLength = puzzleAnswer.Length - 2;
                                        puzzleAnswer = puzzleAnswer.Substring(1, puzzleAndswerLength);

                                        string puzzle = "----Запущен режим игры----"
                                            + "\n----Единственная доступная команда: 'посмотреть ответ'----"
                                            + desiredPuzzle.InnerText;

                                        callbackMessageText = puzzle;
                                        break;
                                    }
                                case "пока":
                                    {
                                        callbackMessageText = "Пока, обязательно возвращайся!";
                                        break;
                                    }
                                case "команды":
                                    {
                                        callbackMessageText = "1. 'привет'  - В ответ бот приветствует вас" +
                                            "\n2. 'загадка' - Бот присылает случайную загадку\n3. 'пока' - Бот прощается с Вами" +
                                            "\n4. 'команды' - доступные Боту команды";
                                        break;
                                    }
                                default:
                                    {
                                        callbackMessageText = msg.Text + "\n P.S. буду тебя передразнивать, пока не введёшь нормальую команду :)" +
                                            "\nЧтобы посмотреть список команд введи 'команды'";
                                        break;
                                    }
                            }
                            // Отправим в ответ полученный от пользователя текст
                            _vkApi.Messages.Send(new MessagesSendParams
                            {
                                RandomId = new DateTime().Millisecond,
                                PeerId = msg.PeerId.Value,
                                Message = callbackMessageText
                            });
                        }


                        break;
                    }
                default:
                    {
                        break;
                    }

            }

            return Ok("ok");
        }

    }
}
