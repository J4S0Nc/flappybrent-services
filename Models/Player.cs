using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Table;

namespace Flappy.Brent.Services.Models
{
    public class PlayerGameModel
    {
        public DateTimeOffset played { get; set; }
        public string deathBy { get; set; }
        public int score { get; set; }
        public int flaps { get; set; }
        public int time { get; set; }
    }
    public class User
    {
        public string id { get; set; }
        public string key { get; set; }
        public string name { get; set; }
    }
    public class PlayerModel
    {
        public User user { get; set; }
        public string hash { get; set; }
        public int highScore { get; set; }
        public int playTime { get; set; }
        public int brentDeaths { get; set; }
        public int fallingDeaths { get; set; }
        public int drinks { get; set; }
        public int songs { get; set; }
        public int flaps { get; set; }
        public IList<PlayerGameModel> games { get; set; }

        public PlayerEntity ToEntity()
        {
            return new PlayerEntity
            {
                RowKey = this.user.id,
                key = this.user.key,
                name = this.user.name,
                lastSeen = DateTimeOffset.UtcNow,
                highScore = this.highScore,
                playTime = this.playTime,
                brentDeaths = this.brentDeaths,
                fallingDeaths = this.fallingDeaths,
                drinks = this.drinks,
                songs = this.songs,
                flaps = this.flaps,
                games = Newtonsoft.Json.JsonConvert.SerializeObject(this.games)
            };
        }
    }
    public class PlayerEntity : TableEntity
    {
        public string key { get; set; }
        public string name { get; set; }
        public DateTimeOffset lastSeen { get; set; }
        public int highScore { get; set; }
        public int playTime { get; set; }
        public int brentDeaths { get; set; }
        public int fallingDeaths { get; set; }
        public int drinks { get; set; }
        public int songs { get; set; }
        public int flaps { get; set; }
        public string games { get; set; } //JSON array of games

        public PlayerModel ToModel(bool includeGames = false)
        {
            PlayerModel model = new PlayerModel
            {
                user = new User
                {
                    id = this.RowKey,
                    //key = this.key, don't bind key! it's for senders only
                    name = this.name,
                },
                highScore = this.highScore,
                playTime = this.playTime,
                brentDeaths = this.brentDeaths,
                fallingDeaths = this.fallingDeaths,
                drinks = this.drinks,
                songs = this.songs,
                flaps = this.flaps,

            };
            if (includeGames)
            {
                model.games = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PlayerGameModel>>(this.games);
            }
            return model;
        }
    }
}