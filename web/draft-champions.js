// Fiche de chaque champion joué par l'utilisateur. Avis général écrit à la main, sans statistiques.
// h chasse les cibles fragiles · k tue les tanks · d plonge dans le combat · i peu mobile · p a besoin d'un frontline
// q craint la mobilité adverse · o craint le poke · l longue portée · g présence sur toute la carte · r roame facilement
// n peu d'impact hors des combats groupés · w peu de prise sur les tanks · e joue loin de l'engage adverse
// t encaisse · c protège ses alliés · E lance les combats
const CHAMPION_TRAITS = {
  "Aatrox":"d", "Ahri":"hre", "Akali":"hdr", "Akshan":"her", "Alistar":"tEc", "Ambessa":"dk", "Amumu":"tE", "Anivia":"ilw", "Annie":"hi", "Aphelios":"ipo",
  "Ashe":"ilE", "Aurelion Sol":"ilk", "Aurora":"er", "Azir":"le", "Bard":"cgr", "Bel'Veth":"dk", "Blitzcrank":"Ei", "Brand":"il", "Braum":"tc", "Briar":"d",
  "Caitlyn":"il", "Camille":"dkg", "Cassiopeia":"ko", "Cho'Gath":"tk", "Corki":"l", "Darius":"dk", "Diana":"hdE", "Dr. Mundo":"tk", "Draven":"ip", "Ekko":"hdr",
  "Elise":"hd", "Evelynn":"hdr", "Ezreal":"le", "Fiddlesticks":"Ei", "Fiora":"dk", "Fizz":"hdr", "Galio":"tEgr", "Gangplank":"gk", "Garen":"td", "Gnar":"tE",
  "Gragas":"tE", "Graves":"hd", "Gwen":"dk", "Hecarim":"dEr", "Heimerdinger":"il", "Hwei":"il", "Illaoi":"d", "Irelia":"dk", "Ivern":"cg", "Janna":"ce",
  "Jarvan IV":"dE", "Jax":"dk", "Jayce":"l", "Jhin":"il", "Jinx":"ip", "K'Sante":"tEk", "Kai'Sa":"dk", "Kalista":"ek", "Karma":"cl", "Karthus":"ign",
  "Kassadin":"hdk", "Katarina":"hdr", "Kayle":"kin", "Kayn":"hd", "Kennen":"dE", "Kha'Zix":"hdr", "Kindred":"ek", "Kled":"dE", "Kog'Maw":"kipon", "LeBlanc":"hre",
  "Lee Sin":"hdE", "Leona":"tE", "Lillia":"eE", "Lissandra":"Ei", "Locke":"hd", "Lucian":"de", "Lulu":"c", "Lux":"il", "Malphite":"tE", "Malzahar":"iw",
  "Maokai":"tEc", "Master Yi":"dk", "Mel":"il", "Milio":"c", "Miss Fortune":"ip", "Mordekaiser":"dk", "Morgana":"c", "Naafiri":"hdr", "Nami":"c", "Nasus":"tk",
  "Nautilus":"tE", "Neeko":"E", "Nidalee":"hle", "Nilah":"dE", "Nocturne":"hdg", "Nunu":"tEr", "Olaf":"dk", "Orianna":"il", "Ornn":"tEk", "Pantheon":"dgE",
  "Poppy":"tc", "Pyke":"hdr", "Qiyana":"hdr", "Quinn":"hre", "Rakan":"cEr", "Rammus":"tE", "Rek'Sai":"dr", "Rell":"tE", "Renata Glasc":"c", "Renekton":"dE",
  "Rengar":"hd", "Riven":"de", "Rumble":"dn", "Ryze":"gk", "Samira":"dp", "Sejuani":"tE", "Senna":"lc", "Seraphine":"cl", "Sett":"dE", "Shaco":"hdr",
  "Shen":"tcg", "Shyvana":"dk", "Singed":"te", "Sion":"tEg", "Sivir":"ep", "Skarner":"tE", "Smolder":"ipk", "Sona":"c", "Soraka":"cg", "Swain":"t",
  "Sylas":"dk", "Syndra":"hi", "Tahm Kench":"tc", "Taliyah":"gr", "Talon":"hdr", "Taric":"tc", "Teemo":"eq", "Thresh":"cE", "Tristana":"dk", "Trundle":"dk",
  "Tryndamere":"dk", "Twisted Fate":"gwe", "Twitch":"hipk", "Udyr":"d", "Urgot":"tk", "Varus":"ilk", "Vayne":"dk", "Veigar":"ikE", "Vel'Koz":"il", "Vex":"Ed",
  "Vi":"dE", "Viego":"dk", "Viktor":"il", "Vladimir":"dk", "Volibear":"dE", "Warwick":"dk", "Wukong":"dE", "Xayah":"ep", "Xerath":"ilqw", "Xin Zhao":"dE",
  "Yasuo":"dk", "Yone":"dk", "Yorick":"gk", "Yunara":"ipk", "Yuumi":"c", "Zaahen":"d", "Zac":"tE", "Zed":"hdr", "Zeri":"ep", "Ziggs":"ilw",
  "Zilean":"cg", "Zoe":"hl", "Zyra":"il"
};
