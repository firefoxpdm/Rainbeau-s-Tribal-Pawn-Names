using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Verse;

namespace RTPN_Code {
	
	[StaticConstructorOnStartup]
	internal static class RTPN_Initializer {
		private static Dictionary<PawnNameCategory, RTPN_NameBank> banks;
		static RTPN_Initializer() {
			Harmony harmony = new Harmony("net.rainbeau.rimworld.mod.pawnnames");
			harmony.PatchAll( Assembly.GetExecutingAssembly() );
			LongEventHandler.QueueLongEvent(Setup, "LibraryStartup", false, null);
		}
		public static void Setup() {
			RTPN_Initializer.banks = new Dictionary<PawnNameCategory, RTPN_NameBank>();
			RTPN_Initializer.banks.Add(PawnNameCategory.HumanStandard, new RTPN_NameBank(PawnNameCategory.HumanStandard));
			RTPN_NameBank nameBank = RTPN_Initializer.BankOf(PawnNameCategory.HumanStandard);
			nameBank.AddNamesFromFile(RTPN_NameSlot.Tribal, Gender.Female, "Tribal_Name_Female.txt");
			nameBank.AddNamesFromFile(RTPN_NameSlot.Tribal, Gender.Male, "Tribal_Name_Male.txt");
			nameBank.AddNamesFromFile(RTPN_NameSlot.Desc, Gender.Male, "Tribal_Adjectives.txt");
			nameBank.AddNamesFromFile(RTPN_NameSlot.Desc, Gender.Female, "Tribal_Colors.txt");
			nameBank.AddNamesFromFile(RTPN_NameSlot.Desc, Gender.None, "Tribal_FactionUnits.txt");
			nameBank.AddNamesFromFile(RTPN_NameSlot.Object, Gender.Female, "Tribal_Animals.txt");
			nameBank.AddNamesFromFile(RTPN_NameSlot.Object, Gender.None, "Tribal_Terrains.txt");
			nameBank.AddNamesFromFile(RTPN_NameSlot.Object, Gender.Male, "Tribal_Weapons.txt");
			foreach (RTPN_NameBank value in RTPN_Initializer.banks.Values) {
				value.ErrorCheck();
			}			
		}
		public static RTPN_NameBank BankOf(PawnNameCategory category) {
			return RTPN_Initializer.banks[category];
		}
	}

	public class RTPN_NameBank {
		public PawnNameCategory nameType;
		private List<string>[,] names;
		private readonly static int numGenders = Enum.GetValues(typeof(Gender)).Length;
		private readonly static int numSlots = Enum.GetValues(typeof(RTPN_NameSlot)).Length;
		string modBasePath = LoadedModManager.RunningMods.First(mcp => mcp.assemblies.loadedAssemblies.Contains(typeof(RTPN_Initializer).Assembly)).RootDir;
		private IEnumerable<List<string>> AllNameLists {
			get {
				for (int i = 0; i < RTPN_NameBank.numGenders; i++) {
					for (int j = 0; j < RTPN_NameBank.numSlots; j++) {
						yield return this.names[i, j];
					}
				}
			}
		}
		public RTPN_NameBank(PawnNameCategory ID) {
			this.nameType = ID;
			this.names = new List<string>[RTPN_NameBank.numGenders, RTPN_NameBank.numSlots];
			for (int i = 0; i < RTPN_NameBank.numGenders; i++) {
				for (int j = 0; j < RTPN_NameBank.numSlots; j++) {
					this.names[i, j] = new List<string>();
				}
			}
		}
		public void AddNames(RTPN_NameSlot slot, Gender gender, IEnumerable<string> namesToAdd) {
			IEnumerator<string> enumerator = namesToAdd.GetEnumerator();
			try {
				while (enumerator.MoveNext()) {
					string current = enumerator.Current;
					this.NamesFor(slot, gender).Add(current);
				}
			}
			finally {
				if (enumerator == null) { }
				enumerator.Dispose();
			}
		}
		public void AddNamesFromFile(RTPN_NameSlot slot, Gender gender, string fileName) {
			string namesPath = Path.GetFullPath(Path.Combine(modBasePath, "Name Lists/")).Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
			this.AddNames(slot, gender, RTPN_FileRead.LinesFromFile(string.Concat(namesPath, fileName)));
 		}
		public void ErrorCheck() {
			IEnumerator<List<string>> enumerator = this.AllNameLists.GetEnumerator();
			try {
				while (enumerator.MoveNext()) {
					List<string> current = enumerator.Current;
					List<string> list = (
					from x in current
					group x by x into g
					where g.Count<string>() > 1
					select g.Key).ToList<string>();
					List<string>.Enumerator enumerator1 = list.GetEnumerator();
					try {
						while (enumerator1.MoveNext()) {
							Log.Error(string.Concat("Duplicated name: ", enumerator1.Current));
						}
					}
					finally {
						((IDisposable)(object)enumerator1).Dispose();
					}
					List<string>.Enumerator enumerator2 = current.GetEnumerator();
					try {
						while (enumerator2.MoveNext()) {
							string str = enumerator2.Current;
							if (str.Trim() == str) {
								continue;
							}
							Log.Error(string.Concat("Trimmable whitespace on name: [", str, "]"));
						}
					}
					finally {
						((IDisposable)(object)enumerator2).Dispose();
					}
				}
			}
			finally {
				if (enumerator == null) { }
				enumerator.Dispose();
			}
		}
		public string GetName(RTPN_NameSlot slot, Gender gender = 0) {
			string str;
			List<string> strs = this.NamesFor(slot, gender);
			int num = 0;
			if (strs.Count == 0) {
				Log.Error(string.Concat(new object[] { "Name list for gender=", gender, " slot=", slot, " is empty." }));
				return "Errorname";
			}
			while (true) {
				str = strs.RandomElement<string>();
				if (!NameUseChecker.NameWordIsUsed(str)) {
					return str;
				}
				num++;
				if (num > 50) {
					break;
				}
			}
			return str;
		}
		public List<string> NamesFor(RTPN_NameSlot slot, Gender gender) {
			return this.names[(int)gender, (int)slot];
		}
	}

	public static class RTPN_FileRead {
		public static IEnumerable<string> LinesFromFile(string filePath) {
			string rawText = GenFile.TextFromRawFile(filePath);
			foreach (string line in GenText.LinesFromString(rawText)) {
				yield return line;
			}		
		}
	}

	public enum RTPN_NameSlot : byte { 
		Tribal,
		Desc,
		Object
	}
	
	[HarmonyPatch (typeof (NameGenerator), "GenerateName", new Type[] {typeof(RulePackDef), typeof(Predicate<string>), typeof(bool), typeof(string), typeof(string) })]
	public static class NameGenerator_GenerateName {
		[HarmonyPriority(Priority.VeryHigh)]
		public static bool Prefix(RulePackDef rootPack, ref string __result, Predicate<string> validator = null) {
			if (rootPack != null && rootPack.defName.Contains("NamerFactionTribal")) {
				RTPN_NameBank nameBank = RTPN_Initializer.BankOf(PawnNameCategory.HumanStandard);
				string name1;
				string name2;
				string name3;
				float format = Rand.Value;
				string factionUnit = nameBank.GetName(RTPN_NameSlot.Desc, Gender.None);
				if (format < 0.25f) { name1 = "The "+factionUnit+" of the "; name3 = ""; }
				else if (format < 0.5f) { name1 = factionUnit+" of the "; name3 = ""; }
				else if (format < 0.75f) { name1 = "The "; name3 = " "+factionUnit; }
				else { name1 = ""; name3 = " "+factionUnit; }
				string subname1;
				string subname2;
				float nickDesc = Rand.Value;
				if (nickDesc < 0.25) { subname1 = nameBank.GetName(RTPN_NameSlot.Desc, Gender.Female); }
				else { subname1 = nameBank.GetName(RTPN_NameSlot.Desc, Gender.Male); }
				float nickObject = Rand.Value;
				if (nickObject < 0.33) { subname2 = nameBank.GetName(RTPN_NameSlot.Object, Gender.Male); }
				else if (nickObject < 0.67) { subname2 = nameBank.GetName(RTPN_NameSlot.Object, Gender.Female); }
				else { subname2 = nameBank.GetName(RTPN_NameSlot.Object, Gender.None); }
				name2 = string.Concat(subname1," ",subname2);
				__result = name1+name2+name3;
				return false;
		    }
			if (rootPack != null && rootPack.defName.Contains("NamerSettlementTribal")) {
				RTPN_NameBank nameBank = RTPN_Initializer.BankOf(PawnNameCategory.HumanStandard);
				string name;
				float format = Rand.Value;
				if (format < 0.25f) { name = nameBank.GetName(RTPN_NameSlot.Tribal, Gender.Female); }
				else if (format < 0.5f) { name = nameBank.GetName(RTPN_NameSlot.Tribal, Gender.Male); }
				else {
					string subname1;
					string subname2;
					float nickDesc = Rand.Value;
					if (nickDesc < 0.25) { subname1 = nameBank.GetName(RTPN_NameSlot.Desc, Gender.Female); }
					else { subname1 = nameBank.GetName(RTPN_NameSlot.Desc, Gender.Male); }
					subname2 = nameBank.GetName(RTPN_NameSlot.Object, Gender.None);
					name = string.Concat(subname1," ",subname2);
				}
				for (int j = 0; j < 100; j++) {
					for (int k = 0; k < 5; k++) {
						string titleCaseSmart1 = name;
						if (j != 0) {
							titleCaseSmart1 = string.Concat(titleCaseSmart1, " ", j + 1);
						}
						if (validator == null || validator(titleCaseSmart1)) {
							__result = titleCaseSmart1;
							return false;
						}
					}
				}
				__result = name;
				return false;
		    }
			return true;
		}
	}
	
	[HarmonyPatch (typeof (PawnBioAndNameGenerator), "GeneratePawnName")]
	public static class PawnBioAndNameGenerator_GeneratePawnName {
		[HarmonyPriority(Priority.VeryHigh)]
		public static bool Prefix(Pawn pawn, ref Name __result, NameStyle style = 0, string forcedLastName = null) {
			if (style != NameStyle.Full) {
				return true;
			}
			RulePackDef nameGenerator = pawn.RaceProps.GetNameGenerator(pawn.gender);
			if (nameGenerator != null) {
				if (nameGenerator.defName.Contains("NamerAnimalGeneric")) {
					if (pawn.Faction != null && (pawn.Faction.def.defName.Contains("Tribe") || pawn.Faction.def.defName == "TribalRaiders")) {
						string name;
						RTPN_NameBank nameBank = RTPN_Initializer.BankOf(PawnNameCategory.HumanStandard);
						name = nameBank.GetName(RTPN_NameSlot.Tribal, pawn.gender);
						if (Rand.Value < 0.33f) {
							string subname1;
							string subname2;
							float nickDesc = Rand.Value;
							if (nickDesc < 0.25) { subname1 = nameBank.GetName(RTPN_NameSlot.Desc, Gender.Female); }
							else { subname1 = nameBank.GetName(RTPN_NameSlot.Desc, Gender.Male); }
							float nickObject = Rand.Value;
							if (nickObject < 0.33) { subname2 = nameBank.GetName(RTPN_NameSlot.Object, Gender.Male); }
							else if (nickObject < 0.67) { subname2 = nameBank.GetName(RTPN_NameSlot.Object, Gender.Female); }
							else { subname2 = nameBank.GetName(RTPN_NameSlot.Object, Gender.None); }
							if (Rand.Value < 0.1) { name = subname2; }
							else { name = string.Concat(subname1," ",subname2); }
						}
		                __result = new NameSingle(name, false);
						return false;
					}
					else {
						return true;
					}
				}
				return true;
			}
			if (pawn.Faction != null && pawn.Faction.def.pawnNameMaker != null) {
				if (pawn.Faction.def.pawnNameMaker.defName.Contains("NamerPersonTribal")) {
					string name1;
					string name2;
					string name3;
					RTPN_NameBank nameBank = RTPN_Initializer.BankOf(PawnNameCategory.HumanStandard);
					name3 = nameBank.GetName(RTPN_NameSlot.Tribal, pawn.gender);
					name1 = nameBank.GetName(RTPN_NameSlot.Tribal, pawn.gender);
					int num = 0;
					do {
						num++;
						if (Rand.Value >= 0.33f) {
							name2 = (Rand.Value >= 0.67f ? name3 : name1);
						}
						else {
							string subname1;
							string subname2;
							float nickDesc = Rand.Value;
							if (nickDesc < 0.25) { subname1 = nameBank.GetName(RTPN_NameSlot.Desc, Gender.Female); }
							else { subname1 = nameBank.GetName(RTPN_NameSlot.Desc, Gender.Male); }
							float nickObject = Rand.Value;
							if (nickObject < 0.33) { subname2 = nameBank.GetName(RTPN_NameSlot.Object, Gender.Male); }
							else if (nickObject < 0.67) { subname2 = nameBank.GetName(RTPN_NameSlot.Object, Gender.Female); }
							else { subname2 = nameBank.GetName(RTPN_NameSlot.Object, Gender.None); }
							if (Rand.Value < 0.1) { name2 = subname2; }
							else { name2 = string.Concat(subname1," ",subname2); }
						}
					}
					while (num < 50 && NameUseChecker.AllPawnsNamesEverUsed.Any<Name>((Name x) => {
						NameTriple nameTriple = x as NameTriple;
						return (nameTriple == null ? false : nameTriple.Nick == name2);
					}));
					name1 = name1+" '"+name2+"'";
					NameTriple fullName = NameTriple.FromString(name1+" "+name3);
					fullName.CapitalizeNick();
					fullName.ResolveMissingPieces(null);
					__result = fullName;
					return false;
				}
			}
			return true;
		}
	}
		
}
