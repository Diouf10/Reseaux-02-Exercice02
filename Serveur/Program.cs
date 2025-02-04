using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

/**
 * classe principale du programme
 *
 * @author Mouhammad Wagane Diouf
 */
class MainClass
{
    public static void Main(string[] args)
    {
        Serveur serveur = new Serveur();
        serveur.StartServeur();
    }
}

internal class Serveur
{
    private Dictionary<string, Socket> clients = new Dictionary<string, Socket>();
    private Socket serverSocket;
    private string ipServeur = "10.0.0.87";
    private int portServeur = 12345;
    
    // Objet de synchronisation
    private readonly object lockObject = new object(); // Objet de synchronisation

    /**
     * Démarre le serveur
     */
    public void StartServeur()
    {
        try
        {
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Parse(ipServeur), portServeur));
            serverSocket.Listen(15);

            Console.WriteLine($"[INFO] Serveur démarré sur {ipServeur}:{portServeur}");

            // Tant que le serveur est démarré, on accepte les connexions
            while (true)
            {
                // Accepter une connexion
                Socket clientSocket = serverSocket.Accept();
                Console.WriteLine($"[INFO] Nouveau client connecté : {clientSocket.RemoteEndPoint}");

                // Démarrer un thread pour gérer le client
                Thread clientThread = new Thread(() => HandleClient(clientSocket));
                clientThread.Start();
            }
        }
		catch (SocketException ex) when (ex.ErrorCode == 10048)
        {
            Console.WriteLine($"[ERREUR] Le port {portServeur} est déjà utilisé.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERREUR] Impossible de démarrer le serveur : {ex.Message}");
        }
    }

    /**
     * Permet de gérer un client
     */
    private void HandleClient(Socket clientSocket)
    {
        string clientName = null!;
        
        try
        {
            // Récupérer le nom du client
            byte[] buffer = new byte[1024];
            int bytesRead = clientSocket.Receive(buffer);
            clientName = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

            if (string.IsNullOrWhiteSpace(clientName))
            {
                Console.WriteLine("[ERREUR] Un client a tenté de se connecter ou s'est deconnecté sans nom.");
                clientSocket.Close();
                return;
            }

            lock (lockObject)
            {
                clients[clientName] = clientSocket;
            }
            Console.WriteLine($"[INFO] {clientName} ({clientSocket.RemoteEndPoint}) a rejoint le chat.");
            
            // Envoyer un message pour informer les autres clients.
            BroadcastMessage($"[INFO] {clientName} a rejoint le chat !", clientName);

            // Tant que le client est connecté, on lit les messages
            while (true)
            {
                bytesRead = clientSocket.Receive(buffer);
                if (bytesRead == 0) break;

                string messageRecu = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"[{clientName}] ({clientSocket.RemoteEndPoint}) : {messageRecu}");

                if (messageRecu.ToLower() == "exit")
                {
                    Console.WriteLine($"[INFO] {clientName} ({clientSocket.RemoteEndPoint}) s'est déconnecté.");
                    break;
                }

                // Envoyer le message à tous les clients
                BroadcastMessage($"[{clientName}] ({clientSocket.RemoteEndPoint}) : {messageRecu}", clientName);
            }
        }
        
        // Gestion de la déconnexion du client, Erreur 10054
        catch (SocketException ex) when (ex.ErrorCode == 10054)
        {
            Console.WriteLine($"[ERREUR] {clientName} s'est déconnecté brutalement.");
        }
        
        // finalement, fermer la connexion
        finally
        {
            if (clientName != null)
            {
                lock (lockObject)
                {
                    if (clients.ContainsKey(clientName))
                    {
                        clients.Remove(clientName);
                    }
                }
            }
            
            // Fermer la connexion
            try
            {
                if (clientSocket.Connected)
                {
                    Console.WriteLine($"[INFO] {clientName} ({clientSocket.RemoteEndPoint}) a quitté le chat.");
                }
            }
            
            // Erreur si le socket est déjà fermé
            catch (ObjectDisposedException)
            {
                Console.WriteLine($"[INFO] {clientName} a quitté le chat (socket déjà fermé).");
            }

            clientSocket.Close();
            BroadcastMessage($"[INFO] {clientName} a quitté le chat.", clientName);
        }
    }

    /**
     * Envoie un message à tous les clients sauf l'expéditeur
     * ps: j'avais déjà vu le broadcast avec Pierre session passée :)
     */
    private void BroadcastMessage(string message, string sender)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        
        // Envoyer le message à tous les clients
        lock (lockObject)
        {
            foreach (var client in clients)
            {
                if (client.Key != sender && client.Value.Connected)
                {
                    try
                    {
                        client.Value.Send(data);
                    }
                    catch (SocketException)
                    {
                        Console.WriteLine($"[ERREUR] Impossible d'envoyer un message à {client.Key}");
                    }
                }
            }
        }
    }
}
