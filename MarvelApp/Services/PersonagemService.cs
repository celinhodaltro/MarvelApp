﻿using MarvelApp.Data;
using MarvelApp.Dto;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;


namespace MarvelApp.Services
{
    public class PersonagemService
    {

        public static void Favoritar(int id, bool retirar = false)
        {

            using (var db = new DataBaseContext())
            {

                var context = db.personagensFavoritos;
                if (retirar == true)
                {
                    context.Remove(context.Where(tb => tb.IdFavorito == id).FirstOrDefault());
                    db.SaveChanges();
                }
                else
                {
                    if (context.Count() >= 5)
                        throw new Exception("Só se pode adicionar 5 personagens favoritos!");
                    else
                    {
                        if (context.Where(tb => tb.IdFavorito == id).Count() != 0)
                            throw new Exception("Personagem ja esta nos favoritos");
                        context.Add(new Favoritos { IdFavorito = id });
                        db.SaveChanges();
                    }
                }

            }


        }

        public static string GerarHashCode(string ts, string publicKey, string privateKey)
        {
            byte[] bytes =
                Encoding.UTF8
                .GetBytes(ts + privateKey + publicKey);
            var gerador = MD5.Create();
            byte[] bytesHash = gerador.ComputeHash(bytes);
            return BitConverter.ToString(bytesHash)
                .ToLower()
                .Replace("-", String.Empty);
        }

        public static List<int> ConsultarIdPersonagensFavoritos()
        {
            using (var db = new DataBaseContext())
            {
                var context = db.personagensFavoritos
                    .Select(tb => tb.IdFavorito)
                    .ToList();
                return context;
            }
        }

        public static personagemPageDto ConsultarPersonagens([Optional] List<int> Favoritos, [Optional] int pagina, [Optional] pesquisaPersonagemDto personagem, [Optional] string ultimaConsulta)
        {
            string privateKey = "df9432e811987a8760d717ac84773a297d87972a";
            string publicKey = "83ee85394fcda89db0b2841c767ee57f";
            string baseUrl = "http://gateway.marvel.com//v1/public/characters";


            personagemPageDto personagemPageDto = new();
            personagem Personagem = new();
            using (var client = new HttpClient())
            {
                if (Favoritos == null)
                    Favoritos = new();
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                string ts = DateTime.Now.Ticks.ToString();
                string hash = PersonagemService.GerarHashCode(ts, publicKey, privateKey);
                var consulta = baseUrl + $"?ts={ts}&apikey={publicKey}&hash={hash}";
                





                if (Favoritos.Count != 0)
                {
                    consulta += "&limit=1";

                    foreach (var favorito in Favoritos)
                    {
                        string novaconsulta = consulta + $"&id={favorito}";

                        HttpResponseMessage responseFavorito = client.GetAsync(novaconsulta).Result;



                        responseFavorito.EnsureSuccessStatusCode();

                        dynamic resultadoFavoritos = JsonConvert.DeserializeObject(responseFavorito.Content.ReadAsStringAsync().Result);
                        Personagem = new();
                        Personagem.ID = resultadoFavoritos.data.results[0].id;
                        Personagem.Nome = resultadoFavoritos.data.results[0].name;
                        Personagem.Descricao = resultadoFavoritos.data.results[0].description;
                        Personagem.URLIMAGEM = resultadoFavoritos.data.results[0].thumbnail.path + "." + resultadoFavoritos.data.results[0].thumbnail.extension;
                        Personagem.URLWIKI = resultadoFavoritos.data.results[0].urls[1].url;
                        personagemPageDto.PersonagensFavoritos.Add(Personagem);

                    }

                    return personagemPageDto;
                }
                else
                {
                    consulta += "&limit=30";

                    if (pagina != 0)
                    {
                        if (ultimaConsulta != null)
                            consulta = ultimaConsulta;

                        personagemPageDto.Pagina = pagina;
                        int contagemIgnorada = pagina * 30;
                        consulta += $"&offset={contagemIgnorada}";
                    }

                    if (personagem != null)
                    {

                        if (personagem.NomeInicial != null)
                            consulta += "&nameStartsWith=" + personagem.NomeInicial;
                        if (personagem.Nome != null)
                            consulta += "&name=" + personagem.Nome;
                        if (personagem.orderBy != "0")
                            consulta += "&orderBy=" + personagem.orderBy;

                    }
                }


                HttpResponseMessage response = client.GetAsync(consulta).Result;



                response.EnsureSuccessStatusCode();
                string conteudo =
                    response.Content.ReadAsStringAsync().Result;

                dynamic resultado = JsonConvert.DeserializeObject(conteudo);

                if (resultado.data.total != null)
                {
                    personagemPageDto.ContagemDePaginas = Convert.ToInt32(resultado.data.total.Value);
                    personagemPageDto.ContagemDePaginas = (personagemPageDto.ContagemDePaginas - (personagemPageDto.ContagemDePaginas % 30)) / 30;
                }

                foreach (var obj in resultado.data.results)
                {
                    Personagem = new();
                    Personagem.ID = obj.id;
                    Personagem.Nome = obj.name;
                    Personagem.Descricao = obj.description;
                    Personagem.URLIMAGEM = obj.thumbnail.path + "." + obj.thumbnail.extension;
                    Personagem.URLWIKI = obj.urls[1].url;
                    personagemPageDto.Personagens.Add(Personagem);
                }

                personagemPageDto.ultimaConsulta = consulta;
                return personagemPageDto;
            }
        }
    }
}
