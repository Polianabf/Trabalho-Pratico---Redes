using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Jogador{
    public string Nome { get; set; }
    public IPEndPoint EndPoint { get; set; }
    public int Pontuacao { get; set; } = 0;
    public bool Finalizou { get; set; } = false;
}

class Servidor{
    static UdpClient server;
    static List<Jogador> jogadores = new List<Jogador>();
    static int maxJogadores = -1;
    static bool partidaIniciada = false;
    static Random rand = new Random();

    static void Main(){
        server = new UdpClient(5000);
        Console.WriteLine("Servidor aguardando configuração na porta 5000...");

        Thread receiveThread = new Thread(ReceberMensagens);
        receiveThread.Start();
    }

    static void ReceberMensagens(){
        while (true){
            IPEndPoint clienteEP = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = server.Receive(ref clienteEP);
            string mensagem = Encoding.UTF8.GetString(data);
            Console.WriteLine($"Recebido de {clienteEP}: {mensagem}");

            ProcessarMensagem(mensagem, clienteEP);
        }
    }

    static void ProcessarMensagem(string msg, IPEndPoint cliente){
        if (msg.StartsWith("CONFIG:") && !partidaIniciada && maxJogadores == -1){
            string[] partes = msg.Split(':');
            if (partes.Length == 3 && int.TryParse(partes[2], out int qtd)){
                maxJogadores = qtd;
                jogadores.Add(new Jogador { Nome = partes[1], EndPoint = cliente });
                Enviar("MENSAGEM:Você configurou a partida. Aguardando outros jogadores...", cliente);
                Console.WriteLine($"{partes[1]} configurou a partida para {qtd} jogadores.");
            }
            else{
                Enviar("MENSAGEM:Erro ao configurar partida. Use CONFIG:<nome>:<quantidade>", cliente);
            }
        }
        else if (msg.StartsWith("ENTRAR:") && !partidaIniciada){
            if (maxJogadores == -1){
                Enviar("MENSAGEM:Partida ainda não configurada. Aguarde o primeiro jogador.", cliente);
                return;
            }

            string nome = msg.Substring(7);
            if (!JogadorExiste(cliente)){
                jogadores.Add(new Jogador { Nome = nome, EndPoint = cliente });
                Enviar($"MENSAGEM:Bem-vindo {nome}! Aguardando mais jogadores...", cliente);
                Console.WriteLine($"{nome} entrou na partida.");

                if (jogadores.Count == maxJogadores)
                    IniciarPartida();
            }
        }
        else if (msg == "PEDIR_CARTA"){
            Jogador jogador = ObterJogador(cliente);
            if (jogador != null && !jogador.Finalizou)
                EnviarCarta(jogador);
        }
        else if (msg == "PARAR"){
            Jogador jogador = ObterJogador(cliente);
            if (jogador != null){
                jogador.Finalizou = true;
                Enviar($"MENSAGEM:{jogador.Nome} parou com {jogador.Pontuacao} pontos.", cliente);
                VerificarFimRodada();
            }
        }
    }

    static void IniciarPartida(){
        partidaIniciada = true;
        Broadcast($"MENSAGEM:Partida iniciada com {maxJogadores} jogadores!");
        foreach (var jogador in jogadores){
            EnviarCarta(jogador);
        }
    }

    static void VerificarFimRodada(){
        bool todosFinalizaram = true;
        foreach (var jogador in jogadores){
            if (!jogador.Finalizou && jogador.Pontuacao <= 21){
                todosFinalizaram = false;
                break;
            }
        }

        if (todosFinalizaram){
            EncerrarRodada();
        }
    }

    static void EncerrarRodada(){
        Broadcast("MENSAGEM:Rodada encerrada. Resultados:");

        int maiorPontuacao = -1;
        List<string> campeoes = new List<string>();

        foreach (var jogador in jogadores){
            string resultado;

            if (jogador.Pontuacao > 21){
                resultado = "Perdeu (Estourou 21)";
            }
            else{
                resultado = $"Parou com {jogador.Pontuacao} pontos";

                if (jogador.Pontuacao > maiorPontuacao){
                    maiorPontuacao = jogador.Pontuacao;
                    campeoes.Clear();
                    campeoes.Add(jogador.Nome);
                }
                else if (jogador.Pontuacao == maiorPontuacao){
                    campeoes.Add(jogador.Nome);
                }
            }

            Enviar($"RESULTADO:{jogador.Nome}:{resultado}", jogador.EndPoint);
        }

        if (campeoes.Count == 0){
            Broadcast("MENSAGEM:Ninguém venceu! Todos estouraram 21.");
        }
        else if (campeoes.Count == 1){
            Broadcast($"MENSAGEM:🏆 Campeão da rodada: {campeoes[0]} com {maiorPontuacao} pontos!");
        }
        else{
            Broadcast($"MENSAGEM:🏆 Empate! Campeões da rodada: {string.Join(", ", campeoes)} com {maiorPontuacao} pontos!");
        }

        jogadores.Clear();
        maxJogadores = -1;
        partidaIniciada = false;
        Console.WriteLine("Servidor pronto para nova configuração.");
    }

    static void EnviarCarta(Jogador jogador){
        int carta = rand.Next(1, 11);
        jogador.Pontuacao += carta;
        Enviar($"CARTA:{carta} TOTAL:{jogador.Pontuacao}", jogador.EndPoint);
        if (jogador.Pontuacao > 21)
        {
            jogador.Finalizou = true;
            Enviar($"RESULTADO:{jogador.Nome}:Perdeu (Estourou 21)", jogador.EndPoint);
            VerificarFimRodada();
        }
    }

    static void Enviar(string mensagem, IPEndPoint cliente){
        byte[] data = Encoding.UTF8.GetBytes(mensagem);
        server.Send(data, data.Length, cliente);
    }

    static void Broadcast(string mensagem){
        foreach (var jogador in jogadores)
            Enviar(mensagem, jogador.EndPoint);
    }

    static Jogador ObterJogador(IPEndPoint cliente){
        foreach (var jogador in jogadores)
            if (jogador.EndPoint.Equals(cliente))
                return jogador;
        return null;
    }

    static bool JogadorExiste(IPEndPoint cliente)
    {
        foreach (var jogador in jogadores)
            if (jogador.EndPoint.Equals(cliente))
                return true;
        return false;
    }
}
