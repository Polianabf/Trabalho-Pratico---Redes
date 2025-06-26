using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Cliente{
    static UdpClient client;
    static IPEndPoint serverEP;
    static string nomeJogador;

    static void Main(){
        client = new UdpClient();
        Console.Write("Digite o IP do servidor (ex: 127.0.0.1): ");
        string ipServidor = Console.ReadLine();
        serverEP = new IPEndPoint(IPAddress.Parse(ipServidor), 5000);

        Console.Write("Você é o primeiro jogador? (S/N): ");
        string resposta = Console.ReadLine()?.Trim().ToUpper();

        if (resposta == "S"){
            Console.Write("Digite seu nome: ");
            nomeJogador = Console.ReadLine();

            Console.Write("Quantos jogadores terão na partida? ");
            string qtd = Console.ReadLine();

            Enviar($"CONFIG:{nomeJogador}:{qtd}");
        }
        else{
            Console.Write("Digite seu nome: ");
            nomeJogador = Console.ReadLine();
            Enviar($"ENTRAR:{nomeJogador}");
        }

        Thread receiveThread = new Thread(ReceberMensagens);
        receiveThread.Start();

        while (true){
            Console.WriteLine("\nComandos disponíveis: PEDIR / PARAR");
            string comando = Console.ReadLine()?.Trim().ToUpper();

            if (comando == "PEDIR")
                Enviar("PEDIR_CARTA");
            else if (comando == "PARAR")
                Enviar("PARAR");
            else
                Console.WriteLine("Comando inválido. Digite PEDIR ou PARAR.");
        }
    }

    static void ReceberMensagens(){
        while (true){
            IPEndPoint anyEP = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = client.Receive(ref anyEP);
            string mensagem = Encoding.UTF8.GetString(data);
            ProcessarMensagem(mensagem);
        }
    }

    static void ProcessarMensagem(string mensagem){
        if (mensagem.StartsWith("CARTA:")){
            Console.WriteLine($"\n=== Carta recebida: {mensagem} ===");
        }
        else if (mensagem.StartsWith("RESULTADO:")){
            Console.WriteLine($"\n### {mensagem} ###\n");
        }
        else if (mensagem.StartsWith("MENSAGEM:")){
            string texto = mensagem.Substring(9);
            Console.WriteLine($"\n[Servidor]: {texto}");

            if (texto.StartsWith("Partida iniciada")){
                Console.WriteLine("\n=== A rodada começou! Use PEDIR ou PARAR ===");
            }
            else if (texto.Contains("aguarde") || texto.Contains("Aguardando")){
                Console.WriteLine("[Aguardando mais jogadores...]\n");
            }
        }
        else{
            Console.WriteLine($"[Recebido]: {mensagem}");
        }
    }

    static void Enviar(string mensagem){
        byte[] data = Encoding.UTF8.GetBytes(mensagem);
        client.Send(data, data.Length, serverEP);
    }
}
