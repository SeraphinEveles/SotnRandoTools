﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.Client.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SotnApi.Constants.Addresses;
using SotnApi.Constants.Values.Alucard;
using SotnApi.Constants.Values.Alucard.Enums;
using SotnApi.Constants.Values.Game;
using SotnApi.Interfaces;
using SotnApi.Models;
using SotnRandoTools.Configuration.Interfaces;
using SotnRandoTools.Constants;
using SotnRandoTools.Khaos.Enums;
using SotnRandoTools.Khaos.Models;
using SotnRandoTools.Services;
using SotnRandoTools.Services.Adapters;
using SotnRandoTools.Utils;
using WatsonWebsocket;

namespace SotnRandoTools.Khaos
{
	public class KhaosController
	{
		private readonly IToolConfig toolConfig;
		private readonly IGameApi gameApi;
		private readonly IAlucardApi alucardApi;
		private readonly IActorApi actorApi;
		private readonly ICheatCollectionAdapter cheats;
		private readonly INotificationService notificationService;
		private readonly IInputService inputService;
		private WatsonWsClient socketClient;

		private string[] lightHelpItems =
		{
			"Leather shield",
			"Shaman shield",
			"Pot Roast",
			"Sirloin",
			"Turkey",
			"Bat Pentagram",
			"Javelin",
			"Luminus",
			"Jewel sword",
			"Icebrand",
			"Holy rod",
			"Star flail",
			"Chakram",
			"Holbein dagger",
			"Heart Refresh",
			"Antivenom",
			"Uncurse",
			"Life apple",
			"Str. potion",
			"Attack potion",
			"Shield potion",
			"Resist fire",
			"Potion",
			"Alucart shield",
			"Alucart sword",
			"Stone mask",
			"Wizard hat",
			"Platinum mail",
			"Diamond plate",
			"Healing mail",
			"Fire mail",
			"Mirror cuirass",
			"Brilliant mail",
			"Axe Lord armor",
			"Alucart mail",
			"Royal cloak",
			"Zircon",
			"Aquamarine",
			"Lapis lazuli",
			"Turquoise",
			"Medal",
			"Crystal cloak"
		};
		private string[] mediumHelpItems =
		{
			"Fire shield",
			"Alucard shield",
			"Cross shuriken",
			"Blood cloak",
			"Buffalo star",
			"Flame star",
			"Estoc",
			"Iron Fist",
			"Gram",
			"Holy sword",
			"Dark Blade",
			"Topaz circlet",
			"Beryl circlet",
			"Ruby circlet",
			"Opal circlet",
			"Coral circlet",
			"Fury plate",
			"Joseph's cloak",
			"Twilight cloak",
			"Moonstone",
			"Onyx",
			"Mystic pendant",
			"Gauntlet",
			"Ring of Feanor",
			"King's stone",
			"Library card",
			"Meal ticket",
			"Goddess shield",
			"Talisman",
			"Herald shield",
			"Walk armor",
			"Garnet",
			"Ring of arcana"
		};
		private string[] heavyHelpItems =
		{
			"Osafune katana",
			"Shield rod",
			"Mourneblade",
			"Iron shield",
			"Medusa shield",
			"Alucard shield",
			"Zweihander",
			"Obsidian sword",
			"Mablung Sword",
			"Masamune",
			"Elixir",
			"Manna prism",
			"Marsil",
			"Fist of Tulkas",
			"Gurthang",
			"Alucard sword",
			"Vorpal blade",
			"Yasatsuna",
			"Library card",
			"Dragon helm",
			"Holy glasses",
			"Spike Breaker",
			"Dark armor",
			"Dracula tunic",
			"God's Garb",
			"Diamond",
			"Ring of Ares",
			"Covenant stone",
			"Gold Ring",
			"Silver Ring",
			"Frankfurter",
			"Opal"

		};
		private string[] progressionRelics =
		{
			"SoulOfBat",
			"SoulOfWolf",
			"FormOfMist",
			"GravityBoots",
			"LeapStone",
			"JewelOfOpen",
			"MermanStatue"
		};
		private string[]? subscribers =
		{
		};

		private List<QueuedAction> queuedActions = new();
		private Queue<MethodInvoker> queuedFastActions = new();
		private Timer actionTimer = new Timer();
		private Timer fastActionTimer = new Timer();

		#region Timers
		private System.Timers.Timer honestGamerTimer = new();
		private System.Timers.Timer subweaponsOnlyTimer = new();
		private System.Timers.Timer crippleTimer = new();
		private System.Timers.Timer bloodManaTimer = new();
		private System.Timers.Timer thirstTimer = new();
		private System.Timers.Timer thirstTickTimer = new();
		private System.Timers.Timer hordeTimer = new();
		private System.Timers.Timer hordeSpawnTimer = new();
		private System.Timers.Timer enduranceSpawnTimer = new();
		private System.Timers.Timer vampireTimer = new();
		private System.Timers.Timer magicianTimer = new();
		private System.Timers.Timer LibraryTimer = new();
		private System.Timers.Timer meltyTimer = new();
		private System.Timers.Timer fourBeastsTimer = new();
		private System.Timers.Timer zawarudoTimer = new();
		private System.Timers.Timer hasteTimer = new();
		private System.Timers.Timer hasteOverdriveTimer = new();
		private System.Timers.Timer hasteOverdriveOffTimer = new();
		#endregion

		private uint hordeZone = 0;
		private uint hordeZone2 = 0;
		private int enduranceCount = 0;
		private uint enduranceRoomX = 0;
		private uint enduranceRoomY = 0;
		private List<Actor> hordeEnemies = new();
		private List<Actor> bannedEnemies = new();
		private List<SearchableActor> bannedHordeEnemies = new List<SearchableActor>
		{
			new SearchableActor {Hp = 60, Damage = 20, Sprite = 63296},  // Warg
			new SearchableActor {Hp = 44, Damage = 7, Sprite = 8820},    // Spellbook
			new SearchableActor {Hp = 66, Damage = 12, Sprite = 11688},  // Magic Tome
			new SearchableActor {Hp = 36, Damage = 6, Sprite = 54040},   // Ectoplasm
			new SearchableActor {Hp = 60, Damage = 16, Sprite = 38652},  // Frozen Shade
			new SearchableActor {Hp = 30, Damage = 20, Sprite = 60380},  // Spectral Weapons
			new SearchableActor {Hp = 48, Damage = 16, Sprite = 16520},  // Slime
			new SearchableActor {Hp = 400, Damage = 35, Sprite = 28812}, // Blue Venus Weed Unflowered
			new SearchableActor {Hp = 1000, Damage = 45, Sprite = 31040}, // Blue Venus Weed Flowered
			new SearchableActor {Hp = 244, Damage = 35, Sprite = 24208},  // Cave Troll
			new SearchableActor {Hp = 200, Damage = 40, Sprite = 9240}	 // Sniper of Goth
		};
		private List<SearchableActor> enduranceBosses = new List<SearchableActor>
		{
			new SearchableActor {Hp = 600, Damage = 6, Sprite = 18296},    // Slogra
			new SearchableActor {Hp = 600, Damage = 7, Sprite = 22392},    // Gaibon
			new SearchableActor {Hp = 200, Damage = 7, Sprite = 14260},    // Doppleganger 10
			new SearchableActor {Hp = 600, Damage = 20, Sprite = 9884},    // Minotaur
			new SearchableActor {Hp = 360, Damage = 20, Sprite = 14428},   // Werewolf
			new SearchableActor {Hp = 1200, Damage = 20, Sprite = 56036},   // Lesser Demon
			new SearchableActor {Hp = 1000, Damage = 20, Sprite = 43920},   // Karasuman
			new SearchableActor {Hp = 1200, Damage = 18, Sprite = 7188},    // Hippogryph
			new SearchableActor {Hp = 666, Damage = 20, Sprite = 54072},   // Olrox
			new SearchableActor {Hp = 2800, Damage = 25, Sprite = 8452},    // Succubus
			new SearchableActor {Hp = 2400, Damage = 20, Sprite = 19772},   // Cerberus
			new SearchableActor {Hp = 2000, Damage = 30, Sprite = 6264},    // Granfaloon
			new SearchableActor {Hp = 100, Damage = 25, Sprite = 27332},   // Richter
			new SearchableActor {Hp = 3200, Damage = 35, Sprite = 40376},   // Darkwing Bat
			new SearchableActor {Hp = 3600, Damage = 30, Sprite = 31032},  // Creature
			new SearchableActor {Hp = 800, Damage = 35, Sprite = 11664},   // Doppleganger 40
			new SearchableActor {Hp = 4444, Damage = 35, Sprite = 46380},   // Death
			new SearchableActor {Hp = 3000, Damage = 35, Sprite = 6044},   // Medusa
			new SearchableActor {Hp = 4000, Damage = 40, Sprite = 16564},  // Akmodan
			new SearchableActor {Hp = 1600, Damage = 9, Sprite = 30724},   // Sypha
			new SearchableActor {Hp = 2000, Damage = 40, Sprite = 43772}   // Shaft
		};
		private SearchableActor shaftActor = new SearchableActor { Hp = 10, Damage = 0, Sprite = 0 };
		private uint storedMana = 0;
		private int spentMana = 0;
		private bool speedLocked = false;
		private bool manaLocked = false;
		private bool invincibilityLocked = false;
		private bool bloodManaActive = false;
		private bool hasteActive = false;
		private bool hasteSpeedOn = false;
		private bool overdriveOn = false;
		private int slowInterval;
		private int normalInterval;
		private int fastInterval;
		private bool shaftHpSet = false;

		public KhaosController(IToolConfig toolConfig, IGameApi gameApi, IAlucardApi alucardApi, IActorApi actorApi, ICheatCollectionAdapter cheats, INotificationService notificationService, IInputService inputService)
		{
			if (toolConfig is null) throw new ArgumentNullException(nameof(toolConfig));
			if (gameApi is null) throw new ArgumentNullException(nameof(gameApi));
			if (alucardApi is null) throw new ArgumentNullException(nameof(alucardApi));
			if (actorApi is null) throw new ArgumentNullException(nameof(actorApi));
			if (cheats == null) throw new ArgumentNullException(nameof(cheats));
			if (notificationService == null) throw new ArgumentNullException(nameof(notificationService));
			if (inputService is null) throw new ArgumentNullException(nameof(inputService));
			this.toolConfig = toolConfig;
			this.gameApi = gameApi;
			this.alucardApi = alucardApi;
			this.actorApi = actorApi;
			this.cheats = cheats;
			this.notificationService = notificationService;
			this.inputService = inputService;

			InitializeTimers();
			notificationService.ActionQueue = queuedActions;
			normalInterval = (int) toolConfig.Khaos.QueueInterval.TotalMilliseconds;
			slowInterval = (int) normalInterval * 2;
			fastInterval = (int) normalInterval / 2;
			Console.WriteLine($"Intervals set. \n normal: {normalInterval / 1000}s, slow:{slowInterval / 1000}s, fast:{fastInterval / 1000}s");

			socketClient = new WatsonWsClient(new Uri(Globals.StreamlabsSocketAddress));
			socketClient.ServerConnected += BotConnected;
			socketClient.ServerDisconnected += BotDisconnected;
			socketClient.MessageReceived += BotMessageReceived;
		}

		public void StartKhaos()
		{
			if (File.Exists(toolConfig.Khaos.NamesFilePath))
			{
				subscribers = FileExtensions.GetLines(toolConfig.Khaos.NamesFilePath);
			}
			actionTimer.Start();
			fastActionTimer.Start();
			if (subscribers is not null && subscribers.Length > 0)
			{
				OverwriteBossNames(subscribers);
			}
			StartCheats();
			socketClient.Start();

			notificationService.AddMessage($"Khaos started");
			Console.WriteLine("Khaos started");
		}
		public void StopKhaos()
		{
			actionTimer.Stop();
			fastActionTimer.Stop();
			Cheat faerieScroll = cheats.GetCheatByName("FaerieScroll");
			faerieScroll.Disable();
			if (socketClient.Connected)
			{
				socketClient.Stop();
			}
			notificationService.AddMessage($"Khaos stopped");
			Console.WriteLine("Khaos stopped");
		}
		public void OverwriteBossNames(string[] subscribers)
		{
			Random rnd = new Random();
			subscribers = subscribers.OrderBy(x => rnd.Next()).ToArray();
			var randomizedBosses = Strings.BossNameAddresses.OrderBy(x => rnd.Next());
			int i = 0;
			foreach (var boss in randomizedBosses)
			{
				if (i == subscribers.Length)
				{
					break;
				}
				gameApi.OverwriteString(boss.Value, subscribers[i]);
				Console.WriteLine($"{boss.Key} renamed to {subscribers[i]}");
				i++;
			}
		}

		#region Khaotic Effects
		public void KhaosStatus(string user = "Khaos")
		{
			uint mapX = alucardApi.MapX;
			uint mapY = alucardApi.MapY;
			bool keepRichterRoom = ((mapX >= 31 && mapX <= 34) && mapY == 8);
			bool succubusRoom = (mapX == 0 && mapY == 0);
			Random rnd = new Random();
			int max = 4;
			if (succubusRoom)
			{
				max = 3;
			}
			int result = rnd.Next(1, max);
			switch (result)
			{
				case 1:
					SpawnPoisonHitbox();
					notificationService.AddMessage($"{user} poisoned you");
					break;
				case 2:
					SpawnCurseHitbox();
					notificationService.AddMessage($"{user} cursed you");
					break;
				case 3:
					SpawnStoneHitbox();
					notificationService.AddMessage($"{user} petrified you");
					break;
				default:
					break;
			}

			Alert("Khaos Status");
		}
		public void KhaosEquipment(string user = "Khaos")
		{
			RandomizeEquipmentSlots();
			notificationService.AddMessage($"{user} used Khaos Equipment");
			Alert("Khaos Equipment");
		}
		public void KhaosStats(string user = "Khaos")
		{
			RandomizeStatsActivate();
			notificationService.AddMessage($"{user} used Khaos Stats");
			Alert("Khaos Stats");
		}
		public void KhaosRelics(string user = "Khaos")
		{
			RandomizeRelicsActivate();
			notificationService.AddMessage($"{user} used Khaos Relics");
			Alert("Khaos Relics");
		}
		public void PandorasBox(string user = "Khaos")
		{
			RandomizeGold();
			RandomizeStatsActivate();
			RandomizeEquipmentSlots();
			RandomizeRelicsActivate();
			RandomizeInventory();
			RandomizeSubweapon();
			gameApi.RespawnBosses();
			notificationService.AddMessage($"{user} opened Pandora's Box");
			Alert("Pandora's Box");
		}
		public void Gamble(string user = "Khaos")
		{
			Random rnd = new Random();
			double goldPercent = rnd.NextDouble();
			uint newGold = (uint) ((double) alucardApi.Gold * goldPercent);
			uint goldSpent = alucardApi.Gold - newGold;
			alucardApi.Gold = newGold;
			string item = Equipment.Items[rnd.Next(1, Equipment.Items.Count)];
			while (item.Contains("empty hand") || item.Contains("-"))
			{
				item = Equipment.Items[rnd.Next(1, Equipment.Items.Count)];
			}
			alucardApi.GrantItemByName(item);


			notificationService.AddMessage($"{user} gambled {goldSpent} gold for {item}");
			Alert("Gamble");
		}
		#endregion
		#region Debuffs
		public void Thirst(string user = "Khaos")
		{
			Cheat darkMetamorphasisCheat = cheats.GetCheatByName("DarkMetamorphasis");
			darkMetamorphasisCheat.PokeValue(1);
			darkMetamorphasisCheat.Enable();
			thirstTimer.Start();
			thirstTickTimer.Start();

			notificationService.AddMessage($"{user} used Thirst");
			notificationService.AddTimer(new Services.Models.ActionTimer
			{
				Name = KhaosActionNames.Thirst,
				Type = Enums.ActionType.Debuff,
				Duration = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.Thirst).FirstOrDefault().Duration
			});
			Alert("Thirst");
		}
		public void Weaken(string user = "Khaos")
		{
			alucardApi.CurrentHp = (uint) (alucardApi.CurrentHp * toolConfig.Khaos.WeakenFactor);
			alucardApi.CurrentMp = (uint) (alucardApi.CurrentHp * toolConfig.Khaos.WeakenFactor);
			alucardApi.CurrentHearts = (uint) (alucardApi.CurrentHp * toolConfig.Khaos.WeakenFactor);
			alucardApi.MaxtHp = (uint) (alucardApi.MaxtHp * toolConfig.Khaos.WeakenFactor);
			alucardApi.MaxtMp = (uint) (alucardApi.MaxtHp * toolConfig.Khaos.WeakenFactor);
			alucardApi.MaxtHearts = (uint) (alucardApi.MaxtHp * toolConfig.Khaos.WeakenFactor);
			alucardApi.Str = (uint) (alucardApi.Str * toolConfig.Khaos.WeakenFactor);
			alucardApi.Con = (uint) (alucardApi.Con * toolConfig.Khaos.WeakenFactor);
			alucardApi.Int = (uint) (alucardApi.Int * toolConfig.Khaos.WeakenFactor);
			alucardApi.Lck = (uint) (alucardApi.Lck * toolConfig.Khaos.WeakenFactor);
			uint newLevel = (uint) (alucardApi.Level * toolConfig.Khaos.WeakenFactor);
			alucardApi.Level = newLevel;
			uint newExperience = 0;
			if (newLevel <= StatsValues.ExperienceValues.Length && newLevel > 1)
			{
				newExperience = (uint) StatsValues.ExperienceValues[(int) newLevel - 1];
			}
			else if (newLevel > 1)
			{
				newExperience = (uint) StatsValues.ExperienceValues[StatsValues.ExperienceValues.Length - 1];
			}
			if (newLevel > 1)
			{
				alucardApi.Level = newLevel;
				alucardApi.Experiecne = newExperience;
			}

			notificationService.AddMessage($"{user} used Weaken");
			Alert("Weaken");
		}
		public void Cripple(string user = "Khaos")
		{
			speedLocked = true;
			SetSpeed(toolConfig.Khaos.CrippleFactor);
			crippleTimer.Start();
			notificationService.AddMessage($"{user} used Cripple");
			notificationService.AddTimer(new Services.Models.ActionTimer
			{
				Name = KhaosActionNames.Cripple,
				Type = Enums.ActionType.Debuff,
				Duration = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.Cripple).FirstOrDefault().Duration
			});
			Alert("Cripple");
		}
		public void BloodMana(string user = "Khaos")
		{
			storedMana = alucardApi.CurrentMp;
			bloodManaActive = true;
			bloodManaTimer.Start();
			manaLocked = true;
			notificationService.AddMessage($"{user} used Blood Mana");
			notificationService.AddTimer(new Services.Models.ActionTimer
			{
				Name = KhaosActionNames.BloodMana,
				Type = Enums.ActionType.Debuff,
				Duration = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.BloodMana).FirstOrDefault().Duration
			});
			Alert("Blood Mana");
		}
		public void HonestGamer(string user = "Khaos")
		{
			alucardApi.TakeRelic(Relic.GasCloud);
			manaLocked = true;
			Cheat manaCheat = cheats.GetCheatByName("Mana");
			manaCheat.PokeValue(5);
			manaCheat.Enable();
			honestGamerTimer.Start();
			notificationService.AddMessage($"{user} used Honest Gamer");
			notificationService.AddTimer(new Services.Models.ActionTimer
			{
				Name = KhaosActionNames.HonestGamer,
				Type = Enums.ActionType.Debuff,
				Duration = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.HonestGamer).FirstOrDefault().Duration
			});
			Alert("Honest Gamer");

		}
		public void SubweaponsOnly(string user = "Khaos")
		{
			Random rnd = new Random();
			int roll = rnd.Next(1, 10);
			while (roll == 6)
			{
				roll = rnd.Next(1, 10);
			}
			alucardApi.Subweapon = (Subweapon) roll;
			alucardApi.CurrentHearts = 200;
			alucardApi.ActivatePotion(Potion.SmartPotion);
			alucardApi.GrantRelic(Relic.CubeOfZoe);
			alucardApi.TakeRelic(Relic.GasCloud);
			Cheat curse = cheats.GetCheatByName("CurseTimer");
			curse.Enable();
			manaLocked = true;
			Cheat manaCheat = cheats.GetCheatByName("Mana");
			manaCheat.PokeValue(5);
			manaCheat.Enable();
			subweaponsOnlyTimer.Start();
			notificationService.AddMessage($"{user} used Subweapons Only");
			notificationService.AddTimer(new Services.Models.ActionTimer
			{
				Name = KhaosActionNames.SubweaponsOnly,
				Type = Enums.ActionType.Debuff,
				Duration = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.SubweaponsOnly).FirstOrDefault().Duration
			});
			Alert("Subweapons Only");
		}
		public void Bankrupt(string user = "Khaos")
		{
			BankruptActivate();
			notificationService.AddMessage($"{user} used Bankrupt");
			Alert("Bankrupt");
		}
		public void RespawnBosses(string user = "Khaos")
		{
			gameApi.RespawnBosses();
			notificationService.AddMessage($"{user} used Respawn Bosses");
			Alert("Respawn Bosses");
		}
		public void Horde(string user = "Khaos")
		{
			hordeTimer.Start();
			hordeSpawnTimer.Start();
			notificationService.AddMessage($"{user} summoned the Horde");
			Alert("Khaos Horde");
		}
		public void Endurance(string user = "Khaos")
		{
			enduranceRoomX = gameApi.MapXPos;
			enduranceRoomY = gameApi.MapYPos;
			enduranceCount++;
			enduranceSpawnTimer.Start();
			notificationService.AddMessage($"{user} used Endurance");
			Alert("Endurance");
		}
		#endregion
		#region Buffs
		public void LightHelp(string user = "Khaos")
		{
			Random rnd = new Random();
			string item = lightHelpItems[rnd.Next(0, lightHelpItems.Length)];
			int rolls = 0;
			while (alucardApi.HasItemInInventory(item) && rolls < 10)
			{
				item = lightHelpItems[rnd.Next(0, lightHelpItems.Length)];
				rolls++;
			}

			int roll = rnd.Next(1, 4);
			switch (roll)
			{
				case 1:
					alucardApi.GrantItemByName(item);
					notificationService.AddMessage($"{user} gave you a {item}");
					break;
				case 2:
					alucardApi.GrantItemByName(item);
					notificationService.AddMessage($"{user} gave you a {item}");
					break;
				case 3:
					alucardApi.GrantItemByName(item);
					notificationService.AddMessage($"{user} gave you a {item}");
					break;
				default:
					break;
			}
			Alert("Light Help");
		}
		public void MediumHelp(string user = "Khaos")
		{
			Random rnd = new Random();
			string item = mediumHelpItems[rnd.Next(0, mediumHelpItems.Length)];
			int rolls = 0;
			while (alucardApi.HasItemInInventory(item) && rolls < 10)
			{
				item = mediumHelpItems[rnd.Next(0, mediumHelpItems.Length)];
				rolls++;
			}

			int roll = rnd.Next(1, 4);
			switch (roll)
			{
				case 1:
					alucardApi.GrantItemByName(item);
					notificationService.AddMessage($"{user} gave you a {item}");
					break;
				case 2:
					alucardApi.GrantItemByName(item);
					notificationService.AddMessage($"{user} gave you a {item}");
					break;
				case 3:
					alucardApi.GrantItemByName(item);
					notificationService.AddMessage($"{user} gave you a {item}");
					break;
				default:
					break;
			}
			Alert("Medium Help");
		}
		public void HeavytHelp(string user = "Khaos")
		{
			Random rnd = new Random();
			string item = heavyHelpItems[rnd.Next(0, heavyHelpItems.Length)];
			int rolls = 0;
			while (alucardApi.HasItemInInventory(item) && rolls < 10)
			{
				item = heavyHelpItems[rnd.Next(0, heavyHelpItems.Length)];
				rolls++;
			}

			int relic = rnd.Next(0, progressionRelics.Length);

			int roll = rnd.Next(1, 4);
			for (int i = 0; i < 11; i++)
			{
				if (!alucardApi.HasRelic((Relic) Enum.Parse(typeof(Relic), progressionRelics[relic])))
				{
					break;
				}
				if (i == 10)
				{
					roll = 1;
					break;
				}
				relic = rnd.Next(0, progressionRelics.Length);
			}

			switch (roll)
			{
				case 1:
					Console.WriteLine(item);
					alucardApi.GrantItemByName(item);
					notificationService.AddMessage($"{user} gave you a {item}");
					break;
				case 2:
					alucardApi.GrantRelic((Relic) Enum.Parse(typeof(Relic), progressionRelics[relic]));
					notificationService.AddMessage($"{user} gave you {(Relic) Enum.Parse(typeof(Relic), progressionRelics[relic])}");
					break;
				case 3:
					Console.WriteLine(item);
					alucardApi.GrantItemByName(item);
					notificationService.AddMessage($"{user} gave you a {item}");
					break;
				default:
					break;
			}
			Alert("Heavy Help");
		}
		public void Vampire(string user = "Khaos")
		{
			Cheat darkMetamorphasisCheat = cheats.GetCheatByName("DarkMetamorphasis");
			darkMetamorphasisCheat.PokeValue(1);
			darkMetamorphasisCheat.Enable();
			Cheat attackPotionCheat = cheats.GetCheatByName("AttackPotion");
			attackPotionCheat.PokeValue(1);
			attackPotionCheat.Enable();
			vampireTimer.Start();
			notificationService.AddMessage($"{user} used Vampire");
			notificationService.AddTimer(new Services.Models.ActionTimer
			{
				Name = KhaosActionNames.Vampire,
				Type = Enums.ActionType.Buff,
				Duration = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.Vampire).FirstOrDefault().Duration
			});
			Alert("Vampire");
		}
		public void BattleOrders(string user = "Khaos")
		{
			alucardApi.CurrentHp = alucardApi.MaxtHp * 2;
			alucardApi.CurrentMp = alucardApi.MaxtMp;
			alucardApi.ActivatePotion(Potion.ShieldPotion);
			notificationService.AddMessage($"{user} used Battle Orders");
			Alert("Battle Orders");
		}
		public void Magician(string user = "Khaos")
		{
			alucardApi.ActivatePotion(Potion.SmartPotion);
			Cheat manaCheat = cheats.GetCheatByName("Mana");
			manaCheat.PokeValue(99);
			manaCheat.Enable();
			manaLocked = true;
			magicianTimer.Start();
			notificationService.AddMessage($"{user} activated Magician mode");
			notificationService.AddTimer(new Services.Models.ActionTimer
			{
				Name = KhaosActionNames.Magician,
				Type = Enums.ActionType.Buff,
				Duration = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.Magician).FirstOrDefault().Duration
			});
			Alert("Magician");
		}
		public void ZaWarudo(string user = "Khaos")
		{
			alucardApi.ActivateStopwatch();
			alucardApi.Subweapon = Subweapon.Stopwatch;

			Cheat stopwatchTimer = cheats.GetCheatByName("SubweaponTimer");
			stopwatchTimer.Enable();
			zawarudoTimer.Start();

			notificationService.AddMessage($"{user} used ZA WARUDO");
			notificationService.AddTimer(new Services.Models.ActionTimer
			{
				Name = KhaosActionNames.ZaWarudo,
				Type = Enums.ActionType.Buff,
				Duration = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.ZaWarudo).FirstOrDefault().Duration
			});
			Alert("ZA WARUDO");
		}
		public void MeltyBlood(string user = "Khaos")
		{
			Cheat width = cheats.GetCheatByName("AlucardAttackHitboxWidth");
			Cheat height = cheats.GetCheatByName("AlucardAttackHitboxHeight");
			width.Enable();
			height.Enable();
			meltyTimer.Start();
			notificationService.AddMessage($"{user} activated Melty Blood");
			notificationService.AddTimer(new Services.Models.ActionTimer
			{
				Name = KhaosActionNames.MeltyBlood,
				Type = Enums.ActionType.Buff,
				Duration = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.MeltyBlood).FirstOrDefault().Duration
			});
			Alert("Melty Blood");
		}
		public void FourBeasts(string user = "Khaos")
		{
			Cheat invincibilityCheat = cheats.GetCheatByName("Invincibility");
			invincibilityCheat.PokeValue(1);
			invincibilityCheat.Enable();
			invincibilityLocked = true;
			Cheat attackPotionCheat = cheats.GetCheatByName("AttackPotion");
			attackPotionCheat.PokeValue(1);
			attackPotionCheat.Enable();
			Cheat shineCheat = cheats.GetCheatByName("Shine");
			shineCheat.PokeValue(1);
			shineCheat.Enable();
			fourBeastsTimer.Start();

			notificationService.AddMessage($"{user} used Four Beasts");
			notificationService.AddTimer(new Services.Models.ActionTimer
			{
				Name = KhaosActionNames.FourBeasts,
				Type = Enums.ActionType.Buff,
				Duration = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.FourBeasts).FirstOrDefault().Duration
			});
			Alert("Four Beasts");
		}
		public void Haste(string user = "Khaos")
		{
			SetHasteStaticSpeeds();
			hasteTimer.Start();
			hasteActive = true;
			speedLocked = true;
			Console.WriteLine($"{user} used {KhaosActionNames.Haste}");
			notificationService.AddMessage($"{user} used {KhaosActionNames.Haste}");
			notificationService.AddTimer(new Services.Models.ActionTimer
			{
				Name = KhaosActionNames.Haste,
				Type = Enums.ActionType.Buff,
				Duration = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.Haste).FirstOrDefault().Duration
			});
			Alert("Haste");
		}
		public void Library(string user = "Khaos")
		{
			alucardApi.ActivatePotion(Potion.SmartPotion);
			Cheat LibraryCheat = cheats.GetCheatByName("Library");
			LibraryCheat.PokeValue(65);
			LibraryCheat.Enable();
			manaLocked = false;
			LibraryTimer.Start();
			notificationService.AddMessage($"{user} Used A Library Card");
			notificationService.AddTimer(new Services.Models.ActionTimer
			{
				Name = KhaosActionNames.Library,
				Type = Enums.ActionType.Khaotic,
				Duration = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.Library).FirstOrDefault().Duration
			});
			Alert("Library");
		}
		#endregion

		public void Update()
		{
			if (gameApi.InAlucardMode() && bloodManaActive)
			{
				CheckManaUsage();
			}
			if (gameApi.InAlucardMode())
			{
				CheckDashInput();
			}
		}
		public void EnqueueAction(EventAddAction eventData)
		{
			if (eventData.Command is null) throw new ArgumentNullException(nameof(eventData.Command));
			if (eventData.Command == "") throw new ArgumentException($"Parameter {nameof(eventData.Command)} is empty!");
			if (eventData.UserName is null) throw new ArgumentNullException(nameof(eventData.UserName));
			if (eventData.UserName == "") throw new ArgumentException($"Parameter {nameof(eventData.UserName)} is empty!");
			string user = eventData.UserName;
			string action = eventData.Command;

			SotnRandoTools.Configuration.Models.Action commandAction;
			switch (action)
			{
				#region Khaotic commands
				case "kstatus":
					commandAction = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.KhaosStatus).FirstOrDefault();
					if (commandAction is not null && commandAction.Enabled)
					{
						queuedFastActions.Enqueue(new MethodInvoker(() => KhaosStatus(user)));
					}
					break;
				case "kequipment":
					commandAction = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.KhaosEquipment).FirstOrDefault();
					if (commandAction is not null && commandAction.Enabled)
					{
						queuedActions.Add(new QueuedAction { Name = "Khaos Equipment", Type = ActionType.Khaotic, Invoker = new MethodInvoker(() => KhaosEquipment(user)) });
					}
					break;
				case "kstats":
					commandAction = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.KhaosStats).FirstOrDefault();
					if (commandAction is not null && commandAction.Enabled)
					{
						queuedActions.Add(new QueuedAction { Name = "Khaos Stats", Type = ActionType.Khaotic, Invoker = new MethodInvoker(() => KhaosStats(user)) });
					}
					break;
				case "krelics":
					commandAction = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.KhaosRelics).FirstOrDefault();
					if (commandAction is not null && commandAction.Enabled)
					{
						queuedActions.Add(new QueuedAction { Name = "Khaos Relics", Type = ActionType.Khaotic, Invoker = new MethodInvoker(() => KhaosRelics(user)) });
					}
					break;
				case "pandora":
					commandAction = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.PandorasBox).FirstOrDefault();
					if (commandAction is not null && commandAction.Enabled)
					{
						queuedActions.Add(new QueuedAction { Name = "Pandora's Box", Type = ActionType.Khaotic, Invoker = new MethodInvoker(() => PandorasBox(user)) });
					}
					break;
				case "gamble":
					commandAction = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.Gamble).FirstOrDefault();
					if (commandAction is not null && commandAction.Enabled)
					{
						queuedFastActions.Enqueue(new MethodInvoker(() => Gamble(user)));
					}
					break;
				case "Library":
					commandAction = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.Library).FirstOrDefault();
					if (commandAction is not null && commandAction.Enabled)
					{
						queuedActions.Add(new QueuedAction { Name = "Library", Type = ActionType.Khaotic, Invoker = new MethodInvoker(() => Library(user)) });
					}
					break;
				#endregion
				#region Debuffs
				case "bankrupt":
					commandAction = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.Bankrupt).FirstOrDefault();
					if (commandAction is not null && commandAction.Enabled)
					{
						queuedActions.Add(new QueuedAction { Name = "Bankrupt", Invoker = new MethodInvoker(() => Bankrupt(user)) });
					}
					break;
				case "weaken":
					commandAction = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.Weaken).FirstOrDefault();
					if (commandAction is not null && commandAction.Enabled)
					{
						queuedActions.Add(new QueuedAction { Name = "Weaken", Invoker = new MethodInvoker(() => Weaken(user)) });
					}
					break;
				case "respawnbosses":
					commandAction = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.RespawnBosses).FirstOrDefault();
					if (commandAction is not null && commandAction.Enabled)
					{
						queuedFastActions.Enqueue(new MethodInvoker(() => RespawnBosses(user)));
					}
					break;
				case "honest":
					commandAction = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.HonestGamer).FirstOrDefault();
					if (commandAction is not null && commandAction.Enabled)
					{
						queuedActions.Add(new QueuedAction { Name = "Honest Gamer", LocksMana = true, Invoker = new MethodInvoker(() => HonestGamer(user)) });
					}
					break;
				case "subsonly":
					commandAction = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.SubweaponsOnly).FirstOrDefault();
					if (commandAction is not null && commandAction.Enabled)
					{
						queuedActions.Add(new QueuedAction { Name = "Subweapons Only", LocksMana = true, Invoker = new MethodInvoker(() => SubweaponsOnly(user)) });
					}
					break;
				case "cripple":
					commandAction = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.Cripple).FirstOrDefault();
					if (commandAction is not null && commandAction.Enabled)
					{
						queuedActions.Add(new QueuedAction { Name = "Cripple", LocksSpeed = true, Invoker = new MethodInvoker(() => Cripple(user)) });
					}
					break;
				case "bloodmana":
					commandAction = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.BloodMana).FirstOrDefault();
					if (commandAction is not null && commandAction.Enabled)
					{
						queuedActions.Add(new QueuedAction { Name = "Blood Mana", LocksMana = true, Invoker = new MethodInvoker(() => BloodMana(user)) });
					}
					break;
				case "thirst":
					commandAction = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.Thirst).FirstOrDefault();
					if (commandAction is not null && commandAction.Enabled)
					{
						queuedActions.Add(new QueuedAction { Name = "Thirst", Invoker = new MethodInvoker(() => Thirst(user)) });
					}
					break;
				case "horde":
					commandAction = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.KhaosHorde).FirstOrDefault();
					if (commandAction is not null && commandAction.Enabled)
					{
						queuedActions.Add(new QueuedAction { Name = "Horde", Invoker = new MethodInvoker(() => Horde(user)) });
					}
					break;
				case "endurance":
					commandAction = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.Endurance).FirstOrDefault();
					if (commandAction is not null && commandAction.Enabled)
					{
						queuedFastActions.Enqueue(new MethodInvoker(() => Endurance(user)));
					}
					break;
				#endregion
				#region Buffs
				case "vampire":
					commandAction = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.Vampire).FirstOrDefault();
					if (commandAction is not null && commandAction.Enabled)
					{
						queuedFastActions.Enqueue(new MethodInvoker(() => Vampire(user)));
					}
					break;
				case "lighthelp":
					commandAction = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.LightHelp).FirstOrDefault();
					if (commandAction is not null && commandAction.Enabled)
					{
						queuedFastActions.Enqueue(new MethodInvoker(() => LightHelp(user)));
					}
					break;
				case "mediumhelp":
					commandAction = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.MediumHelp).FirstOrDefault();
					if (commandAction is not null && commandAction.Enabled)
					{
						queuedFastActions.Enqueue(new MethodInvoker(() => MediumHelp(user)));
					}
					break;
				case "heavyhelp":
					commandAction = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.HeavyHelp).FirstOrDefault();
					if (commandAction is not null && commandAction.Enabled)
					{
						queuedFastActions.Enqueue(new MethodInvoker(() => HeavytHelp(user)));
					}
					break;
				case "battleorders":
					commandAction = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.BattleOrders).FirstOrDefault();
					if (commandAction is not null && commandAction.Enabled)
					{
						queuedActions.Add(new QueuedAction { Name = "Battle Orders", Type = ActionType.Buff, Invoker = new MethodInvoker(() => BattleOrders(user)) });
					}
					break;
				case "magician":
					commandAction = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.Magician).FirstOrDefault();
					if (commandAction is not null && commandAction.Enabled)
					{
						queuedActions.Add(new QueuedAction { Name = "Magician", Type = ActionType.Buff, LocksMana = true, Invoker = new MethodInvoker(() => Magician(user)) });
					}
					break;
				case "melty":
					commandAction = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.MeltyBlood).FirstOrDefault();
					if (commandAction is not null && commandAction.Enabled)
					{
						queuedActions.Add(new QueuedAction { Name = "MeltyBlood", Type = ActionType.Buff, Invoker = new MethodInvoker(() => MeltyBlood(user)) });
					}
					break;
				case "fourbeasts":
					commandAction = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.FourBeasts).FirstOrDefault();
					if (commandAction is not null && commandAction.Enabled)
					{
						queuedActions.Add(new QueuedAction { Name = "Four Beasts", Type = ActionType.Buff, LocksInvincibility = true, Invoker = new MethodInvoker(() => FourBeasts(user)) });
					}
					break;
				case "zawarudo":
					commandAction = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.ZaWarudo).FirstOrDefault();
					if (commandAction is not null && commandAction.Enabled)
					{
						queuedFastActions.Enqueue(new MethodInvoker(() => ZaWarudo(user)));
					}
					break;
				case "haste":
					commandAction = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.Haste).FirstOrDefault();
					if (commandAction is not null && commandAction.Enabled)
					{
						queuedActions.Add(new QueuedAction { Name = "Haste", Type = ActionType.Buff, LocksSpeed = true, Invoker = new MethodInvoker(() => Haste(user)) });
					}
					break;
				default:
					break;
					#endregion
			}
		}
		private void InitializeTimers()
		{
			fastActionTimer.Tick += ExecuteFastAction;
			fastActionTimer.Interval = 2 * (1 * 1000);
			actionTimer.Tick += ExecuteAction;
			actionTimer.Interval = 2 * (1 * 1000);

			honestGamerTimer.Elapsed += HonestGamerOff;
			honestGamerTimer.Interval = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.HonestGamer).FirstOrDefault().Duration.TotalMilliseconds; ;
			subweaponsOnlyTimer.Elapsed += SubweaponsOnlyOff;
			subweaponsOnlyTimer.Interval = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.SubweaponsOnly).FirstOrDefault().Duration.TotalMilliseconds;
			crippleTimer.Elapsed += CrippleOff;
			crippleTimer.Interval = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.Cripple).FirstOrDefault().Duration.TotalMilliseconds;
			bloodManaTimer.Elapsed += BloodManaOff;
			bloodManaTimer.Interval = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.BloodMana).FirstOrDefault().Duration.TotalMilliseconds;
			thirstTimer.Elapsed += ThirstOff;
			thirstTimer.Interval = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.Thirst).FirstOrDefault().Duration.TotalMilliseconds;
			thirstTickTimer.Elapsed += ThirstDrain;
			thirstTickTimer.Interval = 1000;
			hordeTimer.Elapsed += HordeOff;
			hordeTimer.Interval = 5 * (60 * 1000);
			hordeSpawnTimer.Elapsed += HordeSpawn;
			hordeSpawnTimer.Interval = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.KhaosHorde).FirstOrDefault().Interval.TotalMilliseconds;
			enduranceSpawnTimer.Elapsed += EnduranceSpawn;
			enduranceSpawnTimer.Interval = 2 * (1000);

			vampireTimer.Elapsed += VampireOff;
			vampireTimer.Interval = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.Vampire).FirstOrDefault().Duration.TotalMilliseconds;
			magicianTimer.Elapsed += MagicianOff;
			magicianTimer.Interval = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.Magician).FirstOrDefault().Duration.TotalMilliseconds;
			LibraryTimer.Elapsed += LibraryOff;
			LibraryTimer.Interval = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.Library).FirstOrDefault().Duration.TotalMilliseconds;
			meltyTimer.Elapsed += MeltyBloodOff;
			meltyTimer.Interval = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.MeltyBlood).FirstOrDefault().Duration.TotalMilliseconds;
			fourBeastsTimer.Elapsed += FourBeastsOff;
			fourBeastsTimer.Interval = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.FourBeasts).FirstOrDefault().Duration.TotalMilliseconds;
			zawarudoTimer.Elapsed += ZawarudoOff;
			zawarudoTimer.Interval = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.ZaWarudo).FirstOrDefault().Duration.TotalMilliseconds;
			hasteTimer.Elapsed += HasteOff;
			hasteTimer.Interval = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.Haste).FirstOrDefault().Duration.TotalMilliseconds;
			hasteOverdriveTimer.Elapsed += OverdriveOn;
			hasteOverdriveTimer.Interval = (2 * 1000);
			hasteOverdriveOffTimer.Elapsed += OverdriveOff;
			hasteOverdriveOffTimer.Interval = (2 * 1000);
		}
		private void ExecuteAction(Object sender, EventArgs e)
		{
			if (queuedActions.Count > 0)
			{
				uint mapX = alucardApi.MapX;
				uint mapY = alucardApi.MapY;
				bool keepRichterRoom = ((mapX >= 31 && mapX <= 34) && mapY == 8);
				if (gameApi.InAlucardMode() && gameApi.CanMenu() && alucardApi.CurrentHp > 0 && !gameApi.CanSave() && !keepRichterRoom)
				{
					int index = 0;
					bool actionUnlocked = true;

					for (int i = 0; i < queuedActions.Count; i++)
					{
						index = i;
						actionUnlocked = true;
						if (queuedActions[i].LocksSpeed && speedLocked)
						{
							actionUnlocked = false;
							continue;
						}
						if (queuedActions[i].LocksMana && manaLocked)
						{
							actionUnlocked = false;
							continue;
						}
						if (queuedActions[i].LocksInvincibility && invincibilityLocked)
						{
							actionUnlocked = false;
							continue;
						}
						break;
					}

					if (actionUnlocked)
					{
						queuedActions[index].Invoker();
						queuedActions.RemoveAt(index);
						SetDynamicInterval();
					}
					else
					{
						Console.WriteLine($"All actions locked. speed: {speedLocked}, invincibility: {invincibilityLocked}, mana: {manaLocked}");
					}
				}
			}
			else
			{
				actionTimer.Interval = 2000;
			}
		}

		private void SetDynamicInterval()
		{
			if (toolConfig.Khaos.DynamicInterval && queuedActions.Count < 3)
			{
				actionTimer.Interval = slowInterval;
				Console.WriteLine($"Interval set to {slowInterval / 1000}s, {actionTimer.Interval}");
			}
			else if (toolConfig.Khaos.DynamicInterval && queuedActions.Count > 8)
			{
				actionTimer.Interval = fastInterval;
				Console.WriteLine($"Interval set to {fastInterval / 1000}s, {actionTimer.Interval}");
			}
			else
			{
				actionTimer.Interval = normalInterval;
				Console.WriteLine($"Interval set to {normalInterval / 1000}s, {actionTimer.Interval}");
			}
		}

		private void ExecuteFastAction(Object sender, EventArgs e)
		{
			uint mapX = alucardApi.MapX;
			uint mapY = alucardApi.MapY;
			Console.WriteLine($"mapx: {mapX}, mapy: {mapY}");
			bool keepRichterRoom = ((mapX >= 31 && mapX <= 34) && mapY == 8);
			if (gameApi.InAlucardMode() && gameApi.CanMenu() && alucardApi.CurrentHp > 0 && !gameApi.CanSave() && !keepRichterRoom)
			{
				shaftHpSet = false;
				if (queuedFastActions.Count > 0)
				{
					queuedFastActions.Dequeue()();
				}
			}
			if (gameApi.InAlucardMode() && gameApi.CanMenu() && alucardApi.CurrentHp > 0 && !gameApi.CanSave() && keepRichterRoom && !shaftHpSet)
			{
				SetShaftHp();
			}
		}

		#region Khaotic events
		private void RandomizeGold()
		{
			Random rnd = new Random();
			uint gold = (uint) rnd.Next(0, 5000);
			uint roll = (uint) rnd.Next(0, 21);
			if (roll > 16 && roll < 20)
			{
				gold = gold * (uint) rnd.Next(1, 11);
			}
			else if (roll > 19)
			{
				gold = gold * (uint) rnd.Next(10, 81);
			}
			alucardApi.Gold = gold;
		}
		private void RandomizeStatsActivate()
		{
			Random rnd = new Random();
			uint maxHp = alucardApi.MaxtHp;
			uint currentHp = alucardApi.CurrentHp;
			uint maxMana = alucardApi.MaxtMp;
			uint currentMana = alucardApi.CurrentMp;
			uint str = alucardApi.Str;
			uint con = alucardApi.Con;
			uint intel = alucardApi.Int;
			uint lck = alucardApi.Lck;
			uint statPool = str + con + intel + lck > 28 ? str + con + intel + lck - 28 : str + con + intel + lck;
			uint offset = (uint) (rnd.NextDouble() * statPool);

			int statPoolRoll = rnd.Next(1, 4);
			if (statPoolRoll == 2)
			{
				statPool = statPool + offset;
			}
			else
			{
				statPool = ((int) statPool - (int) offset) < 0 ? 0 : statPool - offset;
			}

			double a = rnd.NextDouble();
			double b = rnd.NextDouble();
			double c = rnd.NextDouble();
			double d = rnd.NextDouble();
			double sum = a + b + c + d;
			double percentageStr = (a / sum);
			double percentageCon = (b / sum);
			double percentageInt = (c / sum);
			double percentageLck = (d / sum);

			alucardApi.Str = (uint) (6 + (statPool * percentageStr));
			alucardApi.Con = (uint) (6 + (statPool * percentageCon));
			alucardApi.Int = (uint) (6 + (statPool * percentageInt));
			alucardApi.Lck = (uint) (6 + (statPool * percentageLck));

			uint pointsPool = maxHp + maxMana > 110 ? maxHp + maxMana - 110 : maxHp + maxMana;
			if (maxHp + maxMana < 110)
			{
				pointsPool = 110;
			}
			offset = (uint) (rnd.NextDouble() * pointsPool);

			int pointsRoll = rnd.Next(1, 4);
			if (pointsRoll == 2)
			{
				pointsPool = pointsPool + offset;
			}
			else
			{
				pointsPool = ((int) pointsPool - (int) offset) < 0 ? 0 : pointsPool - offset;
			}

			double hpPercent = rnd.NextDouble();
			uint pointsHp = 80 + (uint) (hpPercent * pointsPool);
			uint pointsMp = 30 + (uint) (pointsPool - (hpPercent * pointsPool));

			if (currentHp > pointsHp)
			{
				alucardApi.CurrentHp = pointsHp;
			}
			if (currentMana > pointsMp)
			{
				alucardApi.CurrentMp = pointsMp;
			}

			alucardApi.MaxtHp = pointsHp;
			alucardApi.MaxtMp = pointsMp;
		}
		private void RandomizeInventory()
		{
			bool hasHolyGlasses = alucardApi.HasItemInInventory("Holy glasses");
			bool hasSpikeBreaker = alucardApi.HasItemInInventory("Spike Breaker");
			bool hasGoldRing = alucardApi.HasItemInInventory("Gold Ring");
			bool hasSilverRing = alucardApi.HasItemInInventory("Silver Ring");

			alucardApi.ClearInventory();
			Random rnd = new Random();

			int itemCount = rnd.Next(toolConfig.Khaos.PandoraMinItems, toolConfig.Khaos.PandoraMaxItems + 1);

			for (int i = 0; i < itemCount; i++)
			{
				int result = rnd.Next(0, Equipment.Items.Count);
				alucardApi.GrantItemByName(Equipment.Items[result]);
			}

			if (hasHolyGlasses)
			{
				alucardApi.GrantItemByName("Holy glasses");
			}
			if (hasSpikeBreaker)
			{
				alucardApi.GrantItemByName("Spike Breaker");
			}
			if (hasGoldRing)
			{
				alucardApi.GrantItemByName("Gold Ring");
			}
			if (hasSilverRing)
			{
				alucardApi.GrantItemByName("Silver Ring");
			}
		}
		private void RandomizeSubweapon()
		{
			Random rnd = new Random();
			var subweapons = Enum.GetValues(typeof(Subweapon));
			alucardApi.Subweapon = (Subweapon) subweapons.GetValue(rnd.Next(subweapons.Length));
		}
		private void RandomizeRelicsActivate()
		{
			Random rnd = new Random();
			var relics = Enum.GetValues(typeof(Relic));
			foreach (var relic in relics)
			{
				int roll = rnd.Next(0, 2);
				if (roll > 0)
				{
					if ((int) relic < 25)
					{
						alucardApi.GrantRelic((Relic) relic);
					}
				}
				else
				{
					alucardApi.TakeRelic((Relic) relic);
				}
			}
		}
		private void RandomizeEquipmentSlots()
		{
			bool equippedHolyGlasses = Equipment.Items[(int) (alucardApi.Helm + Equipment.HandCount + 1)] == "Holy glasses";
			bool equippedSpikeBreaker = Equipment.Items[(int) (alucardApi.Armor + Equipment.HandCount + 1)] == "Spike Breaker";
			bool equippedGoldRing = Equipment.Items[(int) (alucardApi.Accessory1 + Equipment.HandCount + 1)] == "Gold Ring" || Equipment.Items[(int) (alucardApi.Accessory2 + Equipment.HandCount + 1)] == "Gold Ring";
			bool equippedSilverRing = Equipment.Items[(int) (alucardApi.Accessory1 + Equipment.HandCount + 1)] == "Silver Ring" || Equipment.Items[(int) (alucardApi.Accessory2 + Equipment.HandCount + 1)] == "Silver Ring";

			Random rnd = new Random();
			alucardApi.RightHand = (uint) rnd.Next(0, Equipment.HandCount + 1);
			alucardApi.LeftHand = (uint) rnd.Next(0, Equipment.HandCount + 1);
			alucardApi.Armor = (uint) rnd.Next(0, Equipment.ArmorCount + 1);
			alucardApi.Helm = Equipment.HelmStart + (uint) rnd.Next(0, Equipment.HelmCount + 1);
			alucardApi.Cloak = Equipment.CloakStart + (uint) rnd.Next(0, Equipment.CloakCount + 1);
			alucardApi.Accessory1 = Equipment.AccessoryStart + (uint) rnd.Next(0, Equipment.AccessoryCount + 1);
			alucardApi.Accessory2 = Equipment.AccessoryStart + (uint) rnd.Next(0, Equipment.AccessoryCount + 1);
			RandomizeSubweapon();

			if (equippedHolyGlasses)
			{
				alucardApi.GrantItemByName("Holy glasses");
			}
			if (equippedSpikeBreaker)
			{
				alucardApi.GrantItemByName("Spike Breaker");
			}
			if (equippedGoldRing)
			{
				alucardApi.GrantItemByName("Gold Ring");
			}
			if (equippedSilverRing)
			{
				alucardApi.GrantItemByName("Silver Ring");
			}
		}
		#endregion
		#region Debuff events
		private void BloodManaUpdate()
		{
			if (spentMana > 0)
			{
				uint currentHp = alucardApi.CurrentHp;
				if (currentHp > spentMana)
				{
					alucardApi.CurrentHp -= (uint) spentMana;
					alucardApi.CurrentMp += (uint) spentMana;
				}
				else
				{
					alucardApi.CurrentHp = 1;
				}
			}
		}
		private void BloodManaOff(Object sender, EventArgs e)
		{
			manaLocked = false;
			bloodManaActive = false;
			bloodManaTimer.Stop();
		}
		private void ThirstDrain(Object sender, EventArgs e)
		{
			if (alucardApi.CurrentHp > toolConfig.Khaos.ThirstDrainPerSecond + 1)
			{
				alucardApi.CurrentHp -= toolConfig.Khaos.ThirstDrainPerSecond;
			}
			else
			{
				alucardApi.CurrentHp = 1;
			}
		}
		private void ThirstOff(Object sender, EventArgs e)
		{
			Cheat darkMetamorphasisCheat = cheats.GetCheatByName("DarkMetamorphasis");
			darkMetamorphasisCheat.Disable();
			thirstTimer.Stop();
			thirstTickTimer.Stop();
		}
		private void HordeOff(Object sender, EventArgs e)
		{
			hordeEnemies.RemoveRange(0, hordeEnemies.Count);
			hordeTimer.Interval = 5 * (60 * 1000);
			hordeTimer.Stop();
			hordeSpawnTimer.Stop();
		}
		private void HordeSpawn(Object sender, EventArgs e)
		{
			Random rnd = new Random();

			uint mapX = alucardApi.MapX;
			uint mapY = alucardApi.MapY;
			bool keepRichterRoom = ((mapX >= 31 && mapX <= 34) && mapY == 8);
			if (!gameApi.InAlucardMode() || !gameApi.CanMenu() || alucardApi.CurrentHp < 5 || gameApi.CanSave() || keepRichterRoom)
			{
				return;
			}

			uint zone = gameApi.Zone;
			uint zone2 = gameApi.Zone2;

			if (hordeZone != zone || hordeZone2 != zone2 || hordeEnemies.Count == 0)
			{
				hordeEnemies.RemoveRange(0, hordeEnemies.Count);
				FindHordeEnemy();
				hordeZone = zone;
				hordeZone2 = zone2;
			}
			else if (hordeEnemies.Count > 0)
			{
				FindHordeEnemy();
				int enemyIndex = rnd.Next(0, hordeEnemies.Count);
				if (hordeTimer.Interval == 5 * (60 * 1000))
				{
					hordeTimer.Stop();
					hordeTimer.Interval = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.KhaosHorde).FirstOrDefault().Duration.TotalMilliseconds;
					notificationService.AddTimer(new Services.Models.ActionTimer
					{
						Name = KhaosActionNames.KhaosHorde,
						Type = Enums.ActionType.Debuff,
						Duration = toolConfig.Khaos.Actions.Where(a => a.Name == KhaosActionNames.KhaosHorde).FirstOrDefault().Duration
					});
					hordeTimer.Start();
				}
				hordeEnemies[enemyIndex].Xpos = (ushort) rnd.Next(10, 245);
				hordeEnemies[enemyIndex].Ypos = (ushort) rnd.Next(10, 245);
				hordeEnemies[enemyIndex].Palette += (ushort) rnd.Next(1, 10);
				actorApi.SpawnActor(hordeEnemies[enemyIndex]);
			}
		}
		private bool FindHordeEnemy()
		{
			Random rnd = new Random();
			long bannedEnemy = actorApi.FindEnemy(gameApi.SecondCastle ? 101 : 36, 10000);
			Actor bannedActor = new Actor(actorApi.GetActor(bannedEnemy));
			if (bannedEnemy > 0 && bannedEnemies.Where(e => e.Hp == bannedActor.Hp && e.Damage == bannedActor.Damage).Count() < 1)
			{
				bannedEnemies.Add(bannedActor);
				Console.WriteLine($"Added banned enemy with hp: {bannedActor.Hp}, damage: {bannedActor.Damage}, sprite: {bannedActor.Sprite}");
			}

			long enemy = actorApi.FindEnemy(10, gameApi.SecondCastle ? 100 : 350, bannedHordeEnemies);

			if (enemy > 0)
			{
				Actor? hordeEnemy = new Actor(actorApi.GetActor(enemy));
				foreach (Actor bannedEnemyActor in bannedEnemies)
				{
					if (hordeEnemy.Damage == bannedEnemyActor.Damage && hordeEnemy.HitboxHeight == bannedEnemyActor.HitboxHeight)
					{
						hordeEnemy = null;
					}
				}
				if (hordeEnemy is not null && hordeEnemies.Where(e => e.Hp == hordeEnemy.Hp && e.Damage == hordeEnemy.Damage).Count() < 1)
				{
					hordeEnemies.Add(hordeEnemy);
					Console.WriteLine($"Added horde enemy with hp: {hordeEnemy.Hp} sprite: {hordeEnemy.Sprite} damage: {hordeEnemy.Damage}");
					return true;
				}
			}

			return false;
		}
		private void HonestGamerOff(Object sender, EventArgs e)
		{
			Cheat manaCheat = cheats.GetCheatByName("Mana");
			manaCheat.Disable();
			manaLocked = false;
			honestGamerTimer.Stop();
		}
		private void SubweaponsOnlyOff(object sender, EventArgs e)
		{
			Cheat curse = cheats.GetCheatByName("CurseTimer");
			curse.Disable();
			manaLocked = false;
			Cheat manaCheat = cheats.GetCheatByName("Mana");
			manaCheat.Disable();
			subweaponsOnlyTimer.Stop();
		}
		private void CrippleOff(Object sender, EventArgs e)
		{
			SetSpeed();
			crippleTimer.Stop();
			speedLocked = false;
		}
		private void EnduranceSpawn(Object sender, EventArgs e)
		{
			uint roomX = gameApi.MapXPos;
			uint roomY = gameApi.MapYPos;

			Random rnd = new Random();
			if ((roomX == enduranceRoomX && roomY == enduranceRoomY) || !gameApi.InAlucardMode() || !gameApi.CanMenu() || alucardApi.CurrentHp < 5)
			{
				return;
			}

			Actor? bossCopy = null;

			long enemy = actorApi.FindActorFrom(enduranceBosses);
			if (enemy > 0)
			{
				LiveActor boss = actorApi.GetLiveActor(enemy);
				boss.Hp *= (ushort) 1.5;

				bossCopy = new Actor(actorApi.GetActor(enemy));
				Console.WriteLine($"Endurance boss found hp: {bossCopy.Hp}, damage: {bossCopy.Damage}, sprite: {bossCopy.Sprite}");

				bossCopy.Xpos = (ushort) rnd.Next(70, 170);
				bossCopy.Palette = (ushort) (bossCopy.Palette + rnd.Next(1, 10));
				bossCopy.Hp *= (ushort) 1.5;
				bossCopy.Damage = (ushort) (1.5 * bossCopy.Damage);
				actorApi.SpawnActor(bossCopy);
				enduranceCount--;
				enduranceRoomX = roomX;
				enduranceRoomY = roomY;
				if (enduranceCount == 0)
				{
					enduranceSpawnTimer.Stop();
				}
			}
			else
			{
				return;
			}
		}
		private void SpawnPoisonHitbox()
		{
			Actor poison = new Actor();
			poison.HitboxHeight = 255;
			poison.HitboxWidth = 255;
			poison.AutoToggle = 1;
			poison.Damage = 10;
			poison.DamageTypeA = (uint) Actors.Poison;
			actorApi.SpawnActor(poison);
		}
		private void SpawnCurseHitbox()
		{
			Actor poison = new Actor();
			poison.HitboxHeight = 255;
			poison.HitboxWidth = 255;
			poison.AutoToggle = 1;
			poison.Damage = 10;
			poison.DamageTypeB = (uint) Actors.Curse;
			actorApi.SpawnActor(poison);
		}
		private void SpawnStoneHitbox()
		{
			Actor poison = new Actor();
			poison.HitboxHeight = 255;
			poison.HitboxWidth = 255;
			poison.AutoToggle = 1;
			poison.Damage = 10;
			poison.DamageTypeA = (uint) Actors.Stone;
			poison.DamageTypeB = (uint) Actors.Stone;
			actorApi.SpawnActor(poison);
		}
		private void BankruptActivate()
		{
			bool hasHolyGlasses = alucardApi.HasItemInInventory("Holy glasses");
			bool hasSpikeBreaker = alucardApi.HasItemInInventory("Spike Breaker");
			bool hasGoldRing = alucardApi.HasItemInInventory("Gold Ring");
			bool hasSilverRing = alucardApi.HasItemInInventory("Silver Ring");
			bool equippedHolyGlasses = Equipment.Items[(int) (alucardApi.Helm + Equipment.HandCount + 1)] == "Holy glasses";
			bool equippedSpikeBreaker = Equipment.Items[(int) (alucardApi.Armor + Equipment.HandCount + 1)] == "Spike Breaker";
			bool equippedGoldRing = Equipment.Items[(int) (alucardApi.Accessory1 + Equipment.HandCount + 1)] == "Gold Ring" || Equipment.Items[(int) (alucardApi.Accessory2 + Equipment.HandCount + 1)] == "Gold Ring";
			bool equippedSilverRing = Equipment.Items[(int) (alucardApi.Accessory1 + Equipment.HandCount + 1)] == "Silver Ring" || Equipment.Items[(int) (alucardApi.Accessory2 + Equipment.HandCount + 1)] == "Silver Ring";


			alucardApi.Gold = 0;
			alucardApi.ClearInventory();
			alucardApi.RightHand = 0;
			alucardApi.LeftHand = 0;
			alucardApi.Helm = Equipment.HelmStart;
			alucardApi.Armor = 0;
			alucardApi.Cloak = Equipment.CloakStart;
			alucardApi.Accessory1 = Equipment.AccessoryStart;
			alucardApi.Accessory2 = Equipment.AccessoryStart;

			if (equippedHolyGlasses || hasHolyGlasses)
			{
				alucardApi.GrantItemByName("Holy glasses");
			}
			if (equippedSpikeBreaker || hasSpikeBreaker)
			{
				alucardApi.GrantItemByName("Spike Breaker");
			}
			if (equippedGoldRing || hasGoldRing)
			{
				alucardApi.GrantItemByName("Gold Ring");
			}
			if (equippedSilverRing || hasSilverRing)
			{
				alucardApi.GrantItemByName("Silver Ring");
			}
		}
		#endregion
		#region Buff events
		private void VampireOff(object sender, System.Timers.ElapsedEventArgs e)
		{
			Cheat darkMetamorphasisCheat = cheats.GetCheatByName("DarkMetamorphasis");
			darkMetamorphasisCheat.PokeValue(1);
			darkMetamorphasisCheat.Disable();
			Cheat attackPotionCheat = cheats.GetCheatByName("AttackPotion");
			attackPotionCheat.PokeValue(1);
			attackPotionCheat.Disable();
			vampireTimer.Stop();
		}
		private void MagicianOff(Object sender, EventArgs e)
		{
			Cheat manaCheat = cheats.GetCheatByName("Mana");
			manaCheat.Disable();
			manaLocked = false;
			magicianTimer.Stop();
		}
		private void LibraryOff(Object sender, EventArgs e)
		{
			Cheat LibraryCheat = cheats.GetCheatByName("Library");
			LibraryCheat.Disable();
			manaLocked = false;
			LibraryTimer.Stop();
		}
		private void MeltyBloodOff(Object sender, EventArgs e)
		{
			Cheat width = cheats.GetCheatByName("AlucardAttackHitboxWidth");
			Cheat height = cheats.GetCheatByName("AlucardAttackHitboxHeight");
			width.Disable();
			height.Disable();
			meltyTimer.Stop();
		}
		private void FourBeastsOff(object sender, System.Timers.ElapsedEventArgs e)
		{
			Cheat invincibilityCheat = cheats.GetCheatByName("Invincibility");
			invincibilityCheat.Disable();
			invincibilityLocked = false;
			Cheat attackPotionCheat = cheats.GetCheatByName("AttackPotion");
			attackPotionCheat.Disable();
			Cheat shineCheat = cheats.GetCheatByName("Shine");
			shineCheat.Disable();
			fourBeastsTimer.Stop();
		}
		private void ZawarudoOff(Object sender, EventArgs e)
		{
			Cheat stopwatchTimer = cheats.GetCheatByName("SubweaponTimer");
			stopwatchTimer.Disable();
			zawarudoTimer.Stop();
		}
		private void HasteOff(Object sender, EventArgs e)
		{
			SetSpeed();
			hasteTimer.Stop();
			hasteActive = false;
			speedLocked = false;
			hasteSpeedOn = false;
			hasteOverdriveOffTimer.Start();
		}
		private void SetHasteStaticSpeeds()
		{
			float factor = toolConfig.Khaos.HasteFactor;
			alucardApi.WingsmashHorizontalSpeed = (uint) (DefaultSpeeds.WingsmashHorizontal * (factor / 2.5));
			alucardApi.WolfDashTopRightSpeed = (sbyte) Math.Floor(DefaultSpeeds.WolfDashTopRight * (factor / 2));
			alucardApi.WolfDashTopLeftSpeed = (sbyte) Math.Ceiling((sbyte) DefaultSpeeds.WolfDashTopLeft * (factor / 2));
			Console.WriteLine("Set speeds:");
			Console.WriteLine($"Wingsmash: {(uint) (DefaultSpeeds.WingsmashHorizontal * (factor / 2.5))}");
			Console.WriteLine($"Wolf dash right: {(sbyte) Math.Floor(DefaultSpeeds.WolfDashTopRight * (factor / 2))}");
			Console.WriteLine($"Wolf dash left: {(sbyte) Math.Ceiling((sbyte) DefaultSpeeds.WolfDashTopLeft * (factor / 2))}");
		}
		private void ToggleHasteDynamicSpeeds(float factor = 1)
		{
			uint horizontalWhole = (uint) (DefaultSpeeds.WalkingWhole * factor);
			uint horizontalFract = (uint) (DefaultSpeeds.WalkingFract * factor);

			alucardApi.WalkingWholeSpeed = horizontalWhole;
			alucardApi.WalkingFractSpeed = horizontalFract;
			alucardApi.JumpingHorizontalWholeSpeed = horizontalWhole;
			alucardApi.JumpingHorizontalFractSpeed = horizontalFract;
			alucardApi.JumpingAttackLeftHorizontalWholeSpeed = (uint) (0xFF - horizontalWhole);
			alucardApi.JumpingAttackLeftHorizontalFractSpeed = horizontalFract;
			alucardApi.JumpingAttackRightHorizontalWholeSpeed = horizontalWhole;
			alucardApi.JumpingAttackRightHorizontalFractSpeed = horizontalFract;
			alucardApi.FallingHorizontalWholeSpeed = horizontalWhole;
			alucardApi.FallingHorizontalFractSpeed = horizontalFract;
		}
		private void OverdriveOn(object sender, System.Timers.ElapsedEventArgs e)
		{
			Cheat VisualEffectPaletteCheat = cheats.GetCheatByName("VisualEffectPalette");
			VisualEffectPaletteCheat.PokeValue(33126);
			VisualEffectPaletteCheat.Enable();
			Cheat VisualEffectTimerCheat = cheats.GetCheatByName("VisualEffectTimer");
			VisualEffectTimerCheat.PokeValue(30);
			VisualEffectTimerCheat.Enable();
			alucardApi.WingsmashHorizontalSpeed = (uint) (DefaultSpeeds.WingsmashHorizontal * (toolConfig.Khaos.HasteFactor / 1.8));
			overdriveOn = true;
			hasteOverdriveTimer.Stop();
		}
		private void OverdriveOff(object sender, System.Timers.ElapsedEventArgs e)
		{
			Cheat VisualEffectPaletteCheat = cheats.GetCheatByName("VisualEffectPalette");
			VisualEffectPaletteCheat.Disable();
			Cheat VisualEffectTimerCheat = cheats.GetCheatByName("VisualEffectTimer");
			VisualEffectTimerCheat.Disable();
			alucardApi.WingsmashHorizontalSpeed = (uint) (DefaultSpeeds.WingsmashHorizontal);
			overdriveOn = false;
			hasteOverdriveOffTimer.Stop();
		}
		#endregion

		private void StartCheats()
		{
			Cheat faerieScroll = cheats.GetCheatByName("FaerieScroll");
			faerieScroll.Enable();
			Cheat soulOrb = cheats.GetCheatByName("SoulOrb");
			soulOrb.Enable();
			Cheat batCardXp = cheats.GetCheatByName("BatCardXp");
			batCardXp.Enable();
			Cheat ghostCardXp = cheats.GetCheatByName("GhostCardXp");
			ghostCardXp.Enable();
			Cheat faerieCardXp = cheats.GetCheatByName("FaerieCardXp");
			faerieCardXp.Enable();
			Cheat demonCardXp = cheats.GetCheatByName("DemonCardXp");
			demonCardXp.Enable();
			Cheat swordCardXp = cheats.GetCheatByName("SwordCardXp");
			swordCardXp.Enable();
			Cheat spriteCardXp = cheats.GetCheatByName("SpriteCardXp");
			spriteCardXp.Enable();
			Cheat noseDevilCardXp = cheats.GetCheatByName("NoseDevilCardXp");
			noseDevilCardXp.Enable();
		}
		private void SetSpeed(float factor = 1)
		{
			bool slow = factor < 1;
			bool fast = factor > 1;

			uint horizontalWhole = (uint) (DefaultSpeeds.WalkingWhole * factor);
			uint horizontalFract = (uint) (DefaultSpeeds.WalkingFract * factor);

			alucardApi.WingsmashHorizontalSpeed = (uint) (DefaultSpeeds.WingsmashHorizontal * factor);
			alucardApi.WalkingWholeSpeed = horizontalWhole;
			alucardApi.WalkingFractSpeed = horizontalFract;
			alucardApi.JumpingHorizontalWholeSpeed = horizontalWhole;
			alucardApi.JumpingHorizontalFractSpeed = horizontalFract;
			alucardApi.JumpingAttackLeftHorizontalWholeSpeed = (uint) (0xFF - horizontalWhole);
			alucardApi.JumpingAttackLeftHorizontalFractSpeed = horizontalFract;
			alucardApi.JumpingAttackRightHorizontalWholeSpeed = horizontalWhole;
			alucardApi.JumpingAttackRightHorizontalFractSpeed = horizontalFract;
			alucardApi.FallingHorizontalWholeSpeed = horizontalWhole;
			alucardApi.FallingHorizontalFractSpeed = horizontalFract;
			alucardApi.WolfDashTopRightSpeed = (sbyte) Math.Floor(DefaultSpeeds.WolfDashTopRight * factor);
			alucardApi.WolfDashTopLeftSpeed = (sbyte) Math.Ceiling(DefaultSpeeds.WolfDashTopLeft * factor);
			alucardApi.BackdashDecel = slow == true ? DefaultSpeeds.BackdashDecelSlow : DefaultSpeeds.BackdashDecel;
			Console.WriteLine($"Set all speeds with factor {factor}");
		}
		private void SetShaftHp()
		{
			long shaftAddress = actorApi.FindActorFrom(new List<SearchableActor> { shaftActor });
			if (shaftAddress > 0)
			{
				LiveActor shaft = actorApi.GetLiveActor(shaftAddress);
				shaft.Hp = 30;
				shaftHpSet = true;
				Console.WriteLine("Found Shaft actor and set HP to 20.");
			}
			else
			{
				return;
			}
		}
		private void CheckManaUsage()
		{
			uint currentMana = alucardApi.CurrentMp;
			spentMana = 0;
			if (currentMana < storedMana)
			{
				spentMana = (int) storedMana - (int) currentMana;
			}
			storedMana = currentMana;
			BloodManaUpdate();
		}
		private void CheckDashInput()
		{
			if (inputService.RegisteredMove(InputKeys.Dash, Globals.UpdateCooldownFrames) && !hasteSpeedOn && hasteActive)
			{
				ToggleHasteDynamicSpeeds(toolConfig.Khaos.HasteFactor);
				hasteSpeedOn = true;
				hasteOverdriveTimer.Start();
			}
			else if (!inputService.ButtonHeld(InputKeys.Forward) && hasteSpeedOn)
			{
				ToggleHasteDynamicSpeeds();
				hasteSpeedOn = false;
				hasteOverdriveTimer.Stop();
				if (overdriveOn)
				{
					hasteOverdriveOffTimer.Start();
				}
			}
		}
		private void CheckExperience()
		{
			uint currentExperiecne = alucardApi.Experiecne;
			//gainedExperiecne = (int) currentExperiecne - (int) storedExperiecne;
			//storedExperiecne = currentExperiecne;
		}
		private void CheckWingsmashActive()
		{
			//bool wingsmashActive = alucardApi.Action == SotnApi.Constants.Values.Alucard.States.Bat;
			//gainedExperiecne = (int) currentExperiecne - (int) storedExperiecne;
			//storedExperiecne = currentExperiecne;
		}
		private void Alert(string actionName)
		{
			if (!toolConfig.Khaos.Alerts)
			{
				return;
			}

			var action = toolConfig.Khaos.Actions.Where(a => a.Name == actionName).FirstOrDefault();

			if (action is not null && action.AlertPath is not null && action.AlertPath != String.Empty)
			{
				notificationService.PlayAlert(action.AlertPath);
			}
		}
		private void BotMessageReceived(object sender, MessageReceivedEventArgs e)
		{
			JObject eventJson = JObject.Parse(Encoding.UTF8.GetString(e.Data));
			Console.WriteLine("Message from bot: \n" + eventJson.ToString());

			if (eventJson["event"] is not null && eventJson["data"] is not null && eventJson["event"].ToString() == Globals.ActionSocketEvent)
			{
				JObject actionData = JObject.Parse(eventJson["data"].ToString().Replace("/", ""));
				if (actionData["Command"] is not null && actionData["UserName"] is not null)
				{
					EnqueueAction(new EventAddAction { Command = actionData["Command"].ToString(), UserName = actionData["UserName"].ToString() });
				}
			}
			else if (eventJson["event"] is not null && eventJson["data"] is not null && eventJson["event"].ToString() == Globals.ConnectedSocketEvent)
			{
				notificationService.AddMessage($"Bot connected");
			}
		}
		private void BotDisconnected(object sender, EventArgs e)
		{
			Console.WriteLine("Bot socket disconnected");
		}
		private void BotConnected(object sender, EventArgs e)
		{
			JObject auth = JObject.FromObject(new
			{
				author = Globals.Author,
				website = Globals.Website,
				api_key = toolConfig.Khaos.BotApiKey,
				events = new string[] { Globals.ActionSocketEvent }
			});
			socketClient.SendAsync(auth.ToString(), System.Net.WebSockets.WebSocketMessageType.Text);
			Console.WriteLine("Bot socket connected, sending authentication");
		}
	}
}
