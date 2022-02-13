using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Blackjack;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Logging;
using Newtonsoft.Json;

namespace Blackjack
{
    class PluginUi : IDisposable
    {
        private readonly Configuration _configuration;

        private readonly ChatGui _chatGui;

        private readonly ClientState _clientState;

        private readonly Vector4 _red = new(255, 0, 0, 1);
        private readonly Vector4 _green = new(0, 255, 0, 1);
        private readonly Vector4 _white = new(255, 255, 255, 1);

        private BjTable CurrentGameState { get; set; } = new();

        // this extra bool exists for ImGui, since you can't ref a property
        private bool _visible = false;

        public bool Visible
        {
            get { return this._visible; }
            set { this._visible = value; }
        }

        private bool _settingsVisible = false;

        public bool SettingsVisible
        {
            get { return this._settingsVisible; }
            set { this._settingsVisible = value; }
        }

        // passing in the image here just for simplicity
        public PluginUi(Configuration configuration, ChatGui chatGui,
            ClientState clientState)
        {
            _configuration = configuration;
            _chatGui = chatGui;
            _clientState = clientState;

            _chatGui.Enable();
            _chatGui.ChatMessage += ChatGuiOnChatMessage;
        }

        public void Dispose()
        {
            _chatGui.ChatMessage -= ChatGuiOnChatMessage;
        }

        private void ChatGuiOnChatMessage(XivChatType type, uint senderid, ref SeString sender, ref SeString message,
            ref bool ishandled)
        {
            var isLocalPlayer = sender.TextValue.Contains(_clientState.LocalPlayer!.Name.TextValue);
            var stringMessage = message.TextValue;
            var targetPlayer = _clientState.LocalPlayer.TargetObject as PlayerCharacter;
            if (!isLocalPlayer || type != XivChatType.Party) return;

            if (stringMessage.ToLower().Contains("new game"))
            {
                CurrentGameState = new BjTable();
            }
            
            if (CurrentGameState.GameOver)
                return;
            
            var parsedName = "";
            if (targetPlayer != null)
                parsedName = targetPlayer.Name + targetPlayer.HomeWorld.GameData?.Name!;

            if (parsedName.Length < 3)
                parsedName = "Dealer";

            switch (stringMessage.ToLower())
            {
                case { } msg when msg.Contains("bets") && !msg.Contains("all bets placed"):
                    if (CurrentGameState.GameOver)
                        return;
                    ProcessBet(parsedName, stringMessage);
                    return;
                case { } msg when msg.Contains("all bets placed"):
                    if (CurrentGameState.GameOver)
                        return;
                    CurrentGameState.Dealer = new BjPlayer() { Name = "Dealer", TotalBet = 0 };
                    return;
                case { } msg when msg.Contains("dealer hits") || msg.Contains("dealers starting card") ||
                                  msg.Contains("reveal the dealers"):
                    if (CurrentGameState.GameOver)
                        return;
                    CurrentGameState.CardForDealer = true;
                    return;
                case { } msg when msg.Contains("dealer stands") || msg.Contains("dealer bust"):
                    if (CurrentGameState.GameOver)
                        return;
                    CurrentGameState.CheckForWinners();
                    return;
                case { } msg when msg.Contains("stands"):
                    if (CurrentGameState.GameOver)
                        return;
                    CurrentGameState.Players.First(p => p.Name == parsedName && p.HasStood == false).HasStood = true;
                    return;
                case { } msg when msg.Contains("split"):
                    if (CurrentGameState.GameOver)
                        return;
                    ProcessSplit(parsedName);
                    CurrentGameState.RollingSplit = true;
                    return;
                case { } msg when msg.Contains("chooses double down"):
                    ProcessDoubleDown(parsedName);
                    return;
                case { } msg when msg.Contains("random!"):
                    var parsedMesssage = stringMessage.Substring(stringMessage.IndexOf(')'),
                        stringMessage.Length - stringMessage.IndexOf(')'));
                    var number = Convert.ToInt32(Regex.Match(parsedMesssage, @"\d+").Value);
                    if (number > 10)
                        number = 10;
                    else if (number == 1)
                        number = 11;

                    ProcessRandom(parsedName, number);
                    return;
            }
        }

        private void ProcessBet(string parsedName, string stringMessage)
        {
            var betAmount = Convert.ToInt32(Regex.Match(stringMessage, @"\d+").Value);
            var tempString = stringMessage.Split("bets")[1].ToLower();
            if (tempString.Contains('k'))
                betAmount *= 1000;
            else if (tempString.Contains('m'))
                betAmount *= 1000000;

            CurrentGameState.Players.Add(new BjPlayer()
            {
                Name = parsedName,
                TotalBet = betAmount
            });
        }

        private void ProcessDoubleDown(string name)
        {
            CurrentGameState.Players.First(p => p.Name == name).TotalBet *= 2;
        }

        private void ProcessRandom(string name, int number)
        {
            if (CurrentGameState.RollingSplit)
            {
                CurrentGameState.TempCardStorage.Add(number);
                if (CurrentGameState.TempCardStorage.Count != 2) return;
                var playerHands = CurrentGameState.Players.Where(p => p.Name == name);

                foreach (var player in playerHands)
                {
                    player.CurrentCards.Add(CurrentGameState.TempCardStorage.RemoveAndGet(0));
                }
                CurrentGameState.TempCardStorage.Clear();
                CurrentGameState.RollingSplit = false;
            }
            else
            {
                if (!CurrentGameState.CardForDealer)
                {
                    var player = CurrentGameState.Players.First(p => p.Name == name && p.IsBust == false && p.HasStood == false);
                    player.CurrentCards.Add(number);
                }
                else
                {
                    var player = CurrentGameState.Dealer;
                    player.CurrentCards.Add(number);
                    CurrentGameState.CardForDealer = false;
                }
            }
        }

        private void ProcessSplit(string target)
        {
            var player = CurrentGameState.Players.First(p => p.Name.Equals(target));
            if (player.CurrentCards.Count != 2)
                return;

            var splitNumber = player.CurrentCards.First();

            player.CurrentCards.Clear();
            player.CurrentCards.Add(splitNumber);
            player.IsSplit = true;

            CurrentGameState.Players.Add(new BjPlayer()
            {
                Name = player.Name,
                CurrentCards = new List<int>()
                {
                    splitNumber
                },
                TotalBet = player.TotalBet,
                IsSplit = true
            });
        }

        public void Draw()
        {
            DrawMainWindow();
        }

        public void DrawMainWindow()
        {
            if (!Visible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(750, 400), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(750, 400), new Vector2(float.MaxValue, float.MaxValue));
            if (ImGui.Begin("Blackjack Game", ref this._visible,
                    ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.Text($"Current Target is: {_clientState.LocalPlayer!.TargetObject?.Name}");

                if (ImGui.BeginTable("Current Players", 3))
                {
                    ImGui.TableSetupColumn("Name");
                    ImGui.TableSetupColumn("Total Bet");
                    ImGui.TableSetupColumn("Current Cards");
                    ImGui.TableHeadersRow();

                    foreach (var player in CurrentGameState.Players)
                    {
                        player.CheckNumbers();

                        ImGui.TableNextColumn();
                        ImGui.Text(player.Name);
                        ImGui.TableNextColumn();
                        ImGui.TextColored(player.HasWon ? _green : player.IsBust ? _red : _white, player.TotalBetAsString());
                        ImGui.TableNextColumn();
                        ImGui.TextColored(player.CurrentCards.Sum() > 21 ? _red : _white, player.CurrentCardsAsString());
                        ImGui.TableNextRow();
                    }

                    CurrentGameState.Dealer.CheckNumbers();

                    ImGui.TableNextColumn();
                    ImGui.Text(CurrentGameState.Dealer.Name);
                    ImGui.TableNextColumn();
                    ImGui.Text(CurrentGameState.Dealer.TotalBetAsString());
                    ImGui.TableNextColumn();
                    ImGui.TextColored(CurrentGameState.Dealer.CurrentCards.Sum() > 21 ? _red : _white,
                        CurrentGameState.Dealer.CurrentCardsAsString());
                    ImGui.TableNextRow();

                    ImGui.EndTable();
                }

                if (ImGui.Button("Reset Table?"))
                {
                    CurrentGameState = new BjTable();
                }

                ImGui.Spacing();
            }

            ImGui.End();
        }
    }

    internal class BjTable
    {
        public bool GameOver = false;
        public bool RollingSplit = false;
        public bool CardForDealer = false;
        public readonly List<int> TempCardStorage = new();
        public List<BjPlayer> Players { get; set; } = new();

        public BjPlayer Dealer { get; set; } = new()
        {
            Name = "Dealer",
            TotalBet = 0
        };

        public void CheckForWinners()
        {
            GameOver = true;

            var dealerTotal = Dealer.CurrentCards.Sum();
            var amountOfCards = Dealer.CurrentCards.Count;

            foreach (var player in Players.FindAll(p => p.HasStood && !p.IsBust))
            {
                if (player.IsBust)
                    continue;

                if (dealerTotal == 21)
                {
                    if (player.CurrentCards.Sum() == dealerTotal && amountOfCards == player.CurrentCards.Count)
                    {
                        player.Pushed = true;
                        PluginLog.Debug("{0} has pushed", player.Name);
                    }
                    else if (player.CurrentCards.Count == 2)
                    {
                        player.HasWon = true;
                        player.NatBlackjack = true;
                        PluginLog.Debug("{0} has won", player.Name);
                    }
                }
                else
                {
                    if (player.CurrentCards.Sum() > dealerTotal || Dealer.IsBust)
                    {
                        player.HasWon = true;

                        if (player.CurrentCards.Sum() == 2 && player.CurrentCards.Count == 2)
                            player.NatBlackjack = true;
                        
                        PluginLog.Debug("{0} has won", player.Name);
                    }
                    else if (player.CurrentCards.Sum() == dealerTotal)
                    {
                        player.Pushed = true;
                        PluginLog.Debug("{0} has pushed", player.Name);
                    }
                }
            }
        }
    }


    internal class BjPlayer
    {
        public string Name { get; set; } = "";
        public double TotalBet { get; set; }
        public List<int> CurrentCards { get; set; } = new();
        public bool IsBust => CurrentCards.Sum() > 21;
        public bool HasStood { get; set; } = false;
        public bool HasWon { get; set; } = false;
        public bool NatBlackjack { get; set; } = false;
        public bool Pushed { get; set; } = false;
        public bool IsSplit = false;

        public string CurrentCardsAsString()
        {
            if (CurrentCards.Count == 0)
                return "";

            return string.Join(" - ", CurrentCards) + " = " + CurrentCards.Sum();
        }

        public void CheckNumbers()
        {
            if (CurrentCards.Count == 0)
                return;

            if (CurrentCards.Sum() > 21 && CurrentCards.Contains(11))
            {
                var indexOfEleven = CurrentCards.IndexOf(11);
                CurrentCards[indexOfEleven] = 1;
            }
        }

        public string TotalBetAsString()
        {
            if (HasWon)
                return TotalBet.ToString("N") + " (" + (TotalBet * (NatBlackjack ? 2.5 : 2)).ToString("N") + ")";

            if (Pushed)
                return TotalBet.ToString("N") + " (PUSH)";

            return TotalBet.ToString("N");
        }
    }

    public static class Extensions
    {
        public static string RemoveSpecialCharacters(this string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' ||
                    c == '_' || c == ' ')
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        public static T RemoveAndGet<T>(this IList<T> list, int index)
        {
            lock (list)
            {
                T value = list[index];
                list.RemoveAt(index);
                return value;
            }
        }
    }
}