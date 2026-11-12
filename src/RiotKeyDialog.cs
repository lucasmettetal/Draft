// Une seule fenêtre pour la clé Riot (UX-5) : historique, rôles des champions et matchups, en une seule saisie.
// La clé reste dans la mémoire du programme le temps des calculs choisis : jamais enregistrée, jamais journalisée,
// jamais transmise à la page. Chaque lecture dit ce qu'elle lit et ce qu'elle garde (collecte minimale).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DraftMid
{
    [Flags]
    public enum RiotTask { None = 0, History = 1, Roles = 2, Matchups = 4 }

    // Textes de la fenêtre, regroupés pour l'extraction du catalogue (native.riot.dialog.*, I18N-3).
    static class RiotDialogText
    {
        public const string Title = "DRAFT : lectures avec ta clé Riot";
        public const string Intro = "Une seule clé pour les lectures que tu coches. Elle reste en mémoire le temps du calcul : jamais enregistrée, jamais journalisée, jamais transmise à la page. DRAFT n'est pas approuvé par Riot Games.";
        public const string Link = "Obtenir une clé sur developer.riotgames.com";
        public const string KeyLabel = "Clé Riot (masquée, jamais enregistrée) :";
        public const string RegionLabel = "Région :";
        public const string TasksLabel = "Lectures à lancer, l'une après l'autre :";
        public const string HistoryTask = "Importer mon historique";
        public const string HistoryKept = "Lu : tes parties classées solo/duo. Gardé sur ce PC : champion, rôle, résultat, KDA, patch et champions adverses. Aucun nom ni identifiant de joueur.";
        public const string RolesTask = "Mettre à jour les rôles des champions (environ 40 minutes)";
        public const string RolesKept = "Lu : 1 000 parties récentes de joueurs classés. Gardé : la part de chaque rôle par champion, sans aucun joueur.";
        public const string MatchupsTask = "Mettre à jour les matchups de ton pool (environ 65 minutes)";
        public const string MatchupsKept = "Lu : des parties des 30 derniers jours, à ton rôle. Gardé : victoires et défaites de ton pool par adversaire, sans aucun joueur.";
        public const string MatchupsNoPool = "Choisis d'abord ton rôle et ton pool dans Mon pool.";
        public const string AccountsLabel = "Comptes (Nom#TAG, séparés par une virgule) :";
        public const string CountLabel = "Parties à lire :";
        public const string Consent = "J'accepte cette lecture";
        public const string Start = "Lancer";
        public const string Cancel = "Annuler";
        public static string Count(int n) { return n + " parties, environ " + Math.Max(1, (int)Math.Ceiling(n * 1.3 / 60)) + " minutes"; }
        public static string MatchupsFor(string role, int pool) { return "Rôle " + role + ", " + pool + (pool > 1 ? " champions" : " champion") + "."; }
    }

    public class RiotKeyDialog : Form
    {
        const int W = 380;
        static readonly int[] Counts = { 100, 200, 500 };
        readonly TextBox keyBox = new TextBox { UseSystemPasswordChar = true, Width = W };
        readonly ComboBox regionBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = W };
        readonly CheckBox historyBox = new CheckBox { AutoSize = true, Text = RiotDialogText.HistoryTask };
        readonly CheckBox rolesBox = new CheckBox { AutoSize = true, Text = RiotDialogText.RolesTask, MaximumSize = new Size(W, 0) };
        readonly CheckBox matchupsBox = new CheckBox { AutoSize = true, Text = RiotDialogText.MatchupsTask, MaximumSize = new Size(W, 0) };
        readonly TextBox accountsBox = new TextBox { Width = W - 20 };
        readonly ComboBox countBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = W - 20 };
        readonly CheckBox consent = new CheckBox { AutoSize = true, Text = RiotDialogText.Consent };
        readonly FlowLayoutPanel historyFields = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false, Margin = new Padding(20, 0, 0, 8) };
        readonly Button start = new Button { Text = RiotDialogText.Start, DialogResult = DialogResult.OK, AutoSize = true, Enabled = false };

        public string Key { get { return keyBox.Text.Trim(); } }
        public RiotRegion SelectedRegion { get { return (RiotRegion)regionBox.SelectedItem; } }
        public int GamesToRead { get { return Counts[countBox.SelectedIndex]; } }
        public bool Consent { get { return consent.Checked; } }
        public List<string> Accounts { get { return accountsBox.Text.Split(',').Select(a => a.Trim()).Where(a => a != "").ToList(); } }
        public RiotTask Tasks { get { return (historyBox.Checked ? RiotTask.History : 0) | (rolesBox.Checked ? RiotTask.Roles : 0) | (matchupsBox.Checked && matchupsBox.Enabled ? RiotTask.Matchups : 0); } }

        // initial : la lecture demandée (bouton ou menu). matchupRole / matchupPool : null si aucun pool n'est choisi.
        public RiotKeyDialog(Icon icon, Action<string> openLink, IEnumerable<string> savedAccounts, RiotTask initial, string matchupRole, int matchupPool)
        {
            Text = RiotDialogText.Title;
            if (icon != null) Icon = icon;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            regionBox.Items.AddRange(RiotRegion.All);
            regionBox.SelectedIndex = 0;
            foreach (int n in Counts) countBox.Items.Add(RiotDialogText.Count(n));
            countBox.SelectedIndex = 0;
            accountsBox.Text = string.Join(", ", savedAccounts ?? new string[0]);

            Func<string, int, Label> label = (text, bottom) => new Label { AutoSize = true, MaximumSize = new Size(W, 0), Margin = new Padding(0, 0, 0, bottom), Text = text };
            Func<string, Label> kept = text => new Label { AutoSize = true, MaximumSize = new Size(W - 20, 0), Margin = new Padding(20, 0, 0, 8), ForeColor = SystemColors.GrayText, Text = text };
            FlowLayoutPanel root = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false, Padding = new Padding(14) };
            LinkLabel link = new LinkLabel { AutoSize = true, Margin = new Padding(0, 0, 0, 10), Text = RiotDialogText.Link };
            link.LinkClicked += (s, e) => openLink("https://developer.riotgames.com/");
            keyBox.Margin = regionBox.Margin = new Padding(0, 0, 0, 10);
            accountsBox.Margin = countBox.Margin = new Padding(0, 0, 0, 6);
            consent.Margin = new Padding(0, 0, 0, 4);
            historyFields.Controls.AddRange(new Control[] { label(RiotDialogText.AccountsLabel, 3), accountsBox, label(RiotDialogText.CountLabel, 3), countBox, consent });
            historyBox.Margin = rolesBox.Margin = matchupsBox.Margin = new Padding(0, 0, 0, 2);
            bool canMatchups = matchupRole != null && matchupPool > 0;
            matchupsBox.Enabled = canMatchups;
            Label matchupsKept = kept(canMatchups ? RiotDialogText.MatchupsFor(matchupRole, matchupPool) + " " + RiotDialogText.MatchupsKept : RiotDialogText.MatchupsNoPool);
            historyBox.Checked = (initial & RiotTask.History) != 0;
            rolesBox.Checked = (initial & RiotTask.Roles) != 0;
            matchupsBox.Checked = canMatchups && (initial & RiotTask.Matchups) != 0;

            FlowLayoutPanel buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Width = W, Anchor = AnchorStyles.Right, Margin = new Padding(0, 6, 0, 0) };
            Button cancel = new Button { Text = RiotDialogText.Cancel, DialogResult = DialogResult.Cancel, AutoSize = true };
            buttons.Controls.Add(start);
            buttons.Controls.Add(cancel);
            EventHandler check = (s, e) =>
            {
                historyFields.Enabled = historyBox.Checked;
                start.Enabled = CanStart();
            };
            foreach (Control c in new Control[] { keyBox, accountsBox }) c.TextChanged += check;
            foreach (CheckBox c in new[] { consent, historyBox, rolesBox, matchupsBox }) c.CheckedChanged += check;
            root.Controls.AddRange(new Control[] {
                label(RiotDialogText.Intro, 8), link, label(RiotDialogText.KeyLabel, 3), keyBox, label(RiotDialogText.RegionLabel, 3), regionBox,
                label(RiotDialogText.TasksLabel, 6), historyBox, kept(RiotDialogText.HistoryKept), historyFields, rolesBox, kept(RiotDialogText.RolesKept), matchupsBox, matchupsKept, buttons });
            Controls.Add(root);
            AcceptButton = start;
            CancelButton = cancel;
            check(null, EventArgs.Empty);
        }

        // Lancer : une clé, au moins une lecture ; pour l'historique, des comptes valides et l'accord.
        bool CanStart()
        {
            if (Key == "" || Tasks == RiotTask.None) return false;
            if ((Tasks & RiotTask.History) != 0 && !(Consent && Accounts.Count > 0 && Accounts.All(HistoryImporter.ValidAccount))) return false;
            return true;
        }

        // Pour les tests : aucun contrôle coupé, et une fenêtre qui tient à l'écran.
        public string LayoutProblem()
        {
            List<string> problems = new List<string>();
            Rectangle screen = Screen.FromControl(this).WorkingArea;
            if (Width > screen.Width || Height > screen.Height) problems.Add("fenêtre plus grande que l'écran");
            Action<Control> walk = null;
            walk = c =>
            {
                foreach (Control k in c.Controls)
                {
                    if (!k.Visible) continue;
                    if (k.Left < 0 || k.Top < 0 || k.Right > c.ClientSize.Width || k.Bottom > c.ClientSize.Height) problems.Add("coupé : " + k.GetType().Name + " " + k.Text);
                    if ((k is Label || k is CheckBox) && (k.Height + 1 < (k is Label ? k.GetPreferredSize(new Size(k.Width, 0)).Height : k.PreferredSize.Height) || (k is CheckBox && k.PreferredSize.Width > k.Width + 1))) problems.Add("texte coupé : " + k.Text);
                    walk(k);
                }
            };
            walk(this);
            return string.Join(" ; ", problems);
        }

        public bool ConsentFits() { return consent.PreferredSize.Width <= W; }

        public void SaveCapture(string path)
        {
            using (Bitmap image = new Bitmap(Width, Height))
            {
                DrawToBitmap(image, new Rectangle(0, 0, Width, Height));
                image.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        // Pour les tests natifs : l'historique coché, le bouton reste bloqué tant que le consentement manque.
        public bool TestCanStart(string account, string key, bool agree)
        {
            historyBox.Checked = true;
            accountsBox.Text = account;
            keyBox.Text = key;
            consent.Checked = agree;
            return start.Enabled;
        }

        // Pour les tests natifs : lectures sans historique (rôles, matchups), seulement la clé.
        public bool TestCanStartTasks(RiotTask tasks, string key)
        {
            historyBox.Checked = (tasks & RiotTask.History) != 0;
            rolesBox.Checked = (tasks & RiotTask.Roles) != 0;
            matchupsBox.Checked = (tasks & RiotTask.Matchups) != 0;
            keyBox.Text = key;
            return start.Enabled;
        }

        public RiotTask TestPreselected() { return Tasks; }

        // Barre de titre sombre, comme la fenêtre principale (UX-7, repli silencieux).
        public bool DarkTitle;
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            DarkTitle = Native.DarkTitleBar(Handle);
        }
        public bool TestMatchupsAvailable() { return matchupsBox.Enabled; }
    }
}
