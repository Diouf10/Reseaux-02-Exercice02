using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

/**
 * Classe principale du programme
 *
 * @author Mouhammad Wagane Diouf
 */
class MainClass
{
    public static void Main(string[] args)
    {
        Client client = new Client();
        client.StartClient();
    }
}

/**
 * Classe Client
 */
internal class Client
{
    private Socket clientSocket;
    private string ipServeur = "10.0.0.87";
    private int portServeur = 12345;
    private bool isRunning = true;

    /**
     * Fonction qui démarre le client
     */
    public void StartClient()
    {
        try
        {
            // Connexion au serveur
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(ipServeur, portServeur);

            
            // Demander le nom du client
            
            Console.Write("Veuillez entrer votre nom : ");
            string nomClient = Console.ReadLine()?.Trim() ?? "";

            while (string.IsNullOrWhiteSpace(nomClient))
            {
                Console.Write("Nom invalide, veuillez réessayer : ");
                nomClient = Console.ReadLine()?.Trim() ?? "";
            }

            // Envoyer uniquement le nom au serveur
            clientSocket.Send(Encoding.UTF8.GetBytes(nomClient));

            // Démarrer un thread pour recevoir les messages du serveur
            Thread receiveThread = new Thread(ReceiveMessages);
            receiveThread.Start();

            // Envoi des messages
            while (isRunning)
            {
                Console.Write($"{nomClient} : ");
                string messageAEnvoyer = Console.ReadLine()!;

                if (string.IsNullOrWhiteSpace(messageAEnvoyer))
                {
                    Console.WriteLine("Message vide, veuillez réessayer.");
                    continue;
                }

                clientSocket.Send(Encoding.UTF8.GetBytes(messageAEnvoyer));

                
                // Si le message est "exit", on arrête la connexion
                if (messageAEnvoyer.ToLower() == "exit")
                {
                    Console.WriteLine("Déconnexion...");
                    isRunning = false;
                    break;
                }
            }

            clientSocket.Close();
        }
        
        // Gestion de la connexion refusée, Erreur 10061
        catch (SocketException ex) when (ex.ErrorCode == 10061)
        {
            Console.WriteLine("[ERREUR] Connexion refusée ! Assurez-vous que le serveur est démarré.");
        }
    }

    /**
     * Fonction qui écoute les messages du serveur
     */
    private void ReceiveMessages()
    {
        byte[] buffer = new byte[1024];

        try
        {
            // Tant que le client est connecté, on lit les messages
            while (isRunning)
            {
                int bytesRead = clientSocket.Receive(buffer, SocketFlags.None);
                
                // Si le serveur ferme la connexion, on arrête la boucle
                if (bytesRead == 0) 
                    break;

                string messageRecu = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                // Afficher le message sans enlever la saisie du client actuel
                Console.WriteLine($"\n{messageRecu}");
                Console.Write("> ");
            }
        }
        // Gestion de la déconnexion du serveur, Erreur 10054
        catch (SocketException ex) when (ex.ErrorCode == 10054)
        {
            Console.WriteLine("[ERREUR] Le serveur a été fermé.");
        }
        // finallment, fermer la connexion
        finally
        {
            isRunning = false;
            clientSocket.Close();
        }
    }
}
