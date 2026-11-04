// Rôles habituels, écrits à la main et propres au projet : t top, j jungle, m mid, a adc, s support, du plus courant au moins courant.
// Un avis général sans statistiques : il ne sert que de repère si aucune donnée n'existe, jamais d'observation.
// Locke et Zaahen sont absents : trop récents pour leur attribuer un rôle sans mesure.
const ROLE_SHEET = {
  "Aatrox":"t", "Ahri":"m", "Akali":"mt", "Akshan":"mt", "Alistar":"s", "Ambessa":"tj", "Amumu":"js", "Anivia":"m", "Annie":"ms", "Aphelios":"a",
  "Ashe":"as", "Aurelion Sol":"m", "Aurora":"mt", "Azir":"m", "Bard":"s", "Bel'Veth":"j", "Blitzcrank":"s", "Brand":"sj", "Braum":"s", "Briar":"j",
  "Caitlyn":"a", "Camille":"t", "Cassiopeia":"mt", "Cho'Gath":"tm", "Corki":"ma", "Darius":"t", "Diana":"jm", "Dr. Mundo":"tj", "Draven":"a", "Ekko":"jm",
  "Elise":"j", "Evelynn":"j", "Ezreal":"a", "Fiddlesticks":"js", "Fiora":"t", "Fizz":"m", "Galio":"ms", "Gangplank":"t", "Garen":"t", "Gnar":"t",
  "Gragas":"jts", "Graves":"j", "Gwen":"tj", "Hecarim":"j", "Heimerdinger":"mts", "Hwei":"ms", "Illaoi":"t", "Irelia":"tm", "Ivern":"j", "Janna":"s",
  "Jarvan IV":"j", "Jax":"tj", "Jayce":"tm", "Jhin":"a", "Jinx":"a", "K'Sante":"t", "Kai'Sa":"a", "Kalista":"a", "Karma":"sm", "Karthus":"jm",
  "Kassadin":"m", "Katarina":"m", "Kayle":"tm", "Kayn":"j", "Kennen":"tm", "Kha'Zix":"j", "Kindred":"j", "Kled":"t", "Kog'Maw":"a", "LeBlanc":"m",
  "Lee Sin":"j", "Leona":"s", "Lillia":"jt", "Lissandra":"m", "Lucian":"am", "Lulu":"s", "Lux":"sm", "Malphite":"ts", "Malzahar":"m", "Maokai":"sj",
  "Master Yi":"j", "Mel":"ms", "Milio":"s", "Miss Fortune":"a", "Mordekaiser":"tj", "Morgana":"sj", "Naafiri":"mj", "Nami":"s", "Nasus":"t", "Nautilus":"s",
  "Neeko":"ms", "Nidalee":"j", "Nilah":"a", "Nocturne":"j", "Nunu":"j", "Olaf":"tj", "Orianna":"m", "Ornn":"t", "Pantheon":"stm", "Poppy":"jts",
  "Pyke":"s", "Qiyana":"mj", "Quinn":"t", "Rakan":"s", "Rammus":"j", "Rek'Sai":"j", "Rell":"s", "Renata Glasc":"s", "Renekton":"t", "Rengar":"jt",
  "Riven":"t", "Rumble":"tm", "Ryze":"mt", "Samira":"a", "Sejuani":"j", "Senna":"sa", "Seraphine":"sa", "Sett":"ts", "Shaco":"js", "Shen":"ts",
  "Shyvana":"j", "Singed":"t", "Sion":"tm", "Sivir":"a", "Skarner":"jt", "Smolder":"am", "Sona":"s", "Soraka":"s", "Swain":"sm", "Sylas":"mt",
  "Syndra":"m", "Tahm Kench":"ts", "Taliyah":"jm", "Talon":"mj", "Taric":"s", "Teemo":"t", "Thresh":"s", "Tristana":"am", "Trundle":"jt", "Tryndamere":"t",
  "Twisted Fate":"m", "Twitch":"a", "Udyr":"jt", "Urgot":"t", "Varus":"a", "Vayne":"at", "Veigar":"ms", "Vel'Koz":"sm", "Vex":"m", "Vi":"j",
  "Viego":"j", "Viktor":"m", "Vladimir":"mt", "Volibear":"jt", "Warwick":"jt", "Wukong":"jt", "Xayah":"a", "Xerath":"sm", "Xin Zhao":"j", "Yasuo":"mt",
  "Yone":"mt", "Yorick":"t", "Yunara":"a", "Yuumi":"s", "Zac":"j", "Zed":"mj", "Zeri":"a", "Ziggs":"am", "Zilean":"s", "Zoe":"m", "Zyra":"s"
};
