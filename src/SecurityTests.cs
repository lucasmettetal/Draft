// Tests de sécurité exécutés avec --selftest (et donc par la CI) : protections vérifiées sur le vrai code, sans réseau externe.
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DraftMid
{
    static class SecurityTests
    {
        public static void Run(Action<bool, string> t)
        {
            // L'exécutable livre l'interface avec sa CSP stricte, sans les assouplissements réservés aux bancs de test.
            Assembly asm = Assembly.GetExecutingAssembly();
            string index;
            using (Stream s = asm.GetManifestResourceStream("web/index.html"))
            using (StreamReader r = new StreamReader(s, Encoding.UTF8)) index = r.ReadToEnd();
            t(index.Contains("http-equiv=\"Content-Security-Policy\"") && index.Contains("default-src 'none'") && index.Contains("frame-src 'none'") && !index.Contains("unsafe-eval"), "Interface livrée : CSP stricte, sans unsafe-eval");
            t(!asm.GetManifestResourceNames().Any(n => n.EndsWith("-tests.html")), "Interface livrée : aucune page de test embarquée");
            // Traduction : catalogue et fonctions chargés par des balises statiques, avant le script principal (I18N-0).
            int cat = index.IndexOf("<script src=\"i18n/fr.js\"></script>"), run = index.IndexOf("<script src=\"draft-i18n.js\"></script>"), main = index.IndexOf("<script>");
            t(cat > 0 && run > cat && main > run && asm.GetManifestResourceNames().Contains("web/i18n/fr.js") && asm.GetManifestResourceNames().Contains("web/draft-i18n.js"), "Interface livrée : catalogue français et traduction chargés par balise, avant le script principal");
            int catEn = index.IndexOf("<script src=\"i18n/en.js\"></script>");
            t(catEn > cat && run > catEn && asm.GetManifestResourceNames().Contains("web/i18n/en.js"), "Interface livrée : catalogue anglais chargé par balise, après le français et avant la traduction (I18N-2)");

            // Une redirection ne doit jamais emporter la clé Riot vers un autre serveur.
            TcpListener first = new TcpListener(IPAddress.Loopback, 0), other = new TcpListener(IPAddress.Loopback, 0);
            first.Start(); other.Start();
            int otherPort = ((IPEndPoint)other.LocalEndpoint).Port, firstPort = ((IPEndPoint)first.LocalEndpoint).Port;
            bool otherHit = false;
            Task.Run(() => { try { using (other.AcceptTcpClient()) otherHit = true; } catch (SocketException) { } catch (ObjectDisposedException) { } catch (InvalidOperationException) { } });
            Task.Run(() =>
            {
                try
                {
                    using (TcpClient c = first.AcceptTcpClient())
                    using (NetworkStream n = c.GetStream())
                    {
                        byte[] buffer = new byte[8192];
                        string head = "";
                        while (!head.Contains("\r\n\r\n")) { int k = n.Read(buffer, 0, buffer.Length); if (k <= 0) break; head += Encoding.ASCII.GetString(buffer, 0, k); }
                        byte[] reply = Encoding.ASCII.GetBytes("HTTP/1.1 302 Found\r\nLocation: http://127.0.0.1:" + otherPort + "/vol\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
                        n.Write(reply, 0, reply.Length);
                    }
                }
                catch (SocketException) { } catch (IOException) { } catch (ObjectDisposedException) { } catch (InvalidOperationException) { }
            });
            RiotReply answer = Task.Run(() => new RiotWebFetcher("RGAPI-cle-de-test").GetAsync("http://127.0.0.1:" + firstPort + "/riot")).GetAwaiter().GetResult();
            Thread.Sleep(300);
            first.Stop(); other.Stop();
            t(answer.Status == 302 && !otherHit, "Clé Riot : une redirection n'est jamais suivie (statut " + answer.Status + ")");
        }
    }
}
