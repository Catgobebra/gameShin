using System;
using System.Collections.Generic;
using BulletGame.Controller;
using BulletGame.Controllers;
using BulletGame.Models;
using BulletGame.Views;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BulletGame
{
    public class Game1 : Game
    {
        private SpawnManager _spawnManager;

        private WaveProcessor _waveProcessor;

        private LevelData _currentLevelData;
        private LevelData currentLevelData;

        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private SpriteFont textBlock;
        private GameRenderer _gameRenderer;

        private PlayerController player;
        private EnemyController enemy;

        private MenuInputHandler _menuInputHandler;

        private SpriteFont miniTextBlock;
        private SpriteFont japanTextBlock;
        private SpriteFont japanSymbol;
        private SpriteFont miniS_TextBlock;

        private BulletManager _bulletManager;

        public Random rnd = new();

        private OptimizedBulletPool _bulletPool;
        private List<EnemyController> _enemies = new List<EnemyController>();
        private List<BonusController> _bonuses = new List<BonusController>();

        private int MaxCountBonus = 1;


        private bool _isWaveInProgress = false;

        public InputHandler _inputHandler;

        public enum GameState { Menu, Playing, Pause, Victory, GameCompleted}

        public GameState _currentState { get; set; } = GameState.Menu;

        public int _selectedMenuItem = 0;
        public string[] MenuItems => _currentState == GameState.Pause
        ? new[] { "Продолжить", "Начать заново", "Выход в меню" }
        : new[] { "Новая игра", "Продолжить", "Выход" };

        private const float MenuItemSpacing = 60f;

        private Stack<object> _enemyWaveStack = new Stack<object>();

        GameplayManager _gameplayManager;

        private Texture2D[] _level1Textures;

        public int Lvl = 1;

        private float _spawnTimer;
        private const float SpawnInterval = 1f;

        private const float PreBattleDelay = 100f;

        private Rectangle _gameArea;
        private Viewport _gameViewport;
        private Viewport _uiViewport;

        private BonusController defaultBonus;

        private UIManager _uiManager;
         
        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            spriteBatch = new SpriteBatch(GraphicsDevice);
            PrimitiveRenderer.Initialize(GraphicsDevice);
            _gameRenderer = new GameRenderer(spriteBatch, GraphicsDevice);

            _level1Textures = new Texture2D[] {
                Content.Load<Texture2D>("ascii-art (2)"),
                Content.Load<Texture2D>("ascii-art"),
                Content.Load<Texture2D>("ascii-art (4)"),
                Content.Load<Texture2D>("ascii-art (3)")
            };
 
            textBlock = Content.Load<SpriteFont>("File");
            miniTextBlock = Content.Load<SpriteFont>("FileMini");
            miniS_TextBlock = Content.Load<SpriteFont>("FileMiniS");
            japanTextBlock = Content.Load<SpriteFont>("Japan");
            japanSymbol = Content.Load<SpriteFont>("JApanS");

            _bulletManager = new BulletManager(
            _bulletPool,
            player,
            _enemies,
            _gameArea
            );

            _uiManager = new UIManager(
               textBlock,
               japanTextBlock,
               miniTextBlock,
               miniS_TextBlock,
               japanSymbol,
               spriteBatch,
               GraphicsDevice,
               player,
               _enemies,
               _bonuses,
               _bulletPool,
               _gameArea,
               _level1Textures
           );

            _gameplayManager = new GameplayManager(
            player,
            _enemies,
            _bonuses,
            _enemyWaveStack,
            _spawnManager,
            _waveProcessor,
            _bulletPool,
            _bulletManager,
            _uiManager,
            _gameArea,
            GraphicsDevice,
            currentLevelData,
            defaultBonus);

            MaxCountBonus = _currentLevelData.MaxBonusCount;

            _spawnManager.InitializeWaveStack(_currentLevelData);
        }

        protected override void Initialize()
        {
            _currentLevelData = LevelLoader.LoadLevel(Lvl);
            currentLevelData = _currentLevelData;

            graphics.PreferredBackBufferWidth = 1920;
            graphics.PreferredBackBufferHeight = 1080;
            graphics.HardwareModeSwitch = true;
            graphics.IsFullScreen = true;
            this.IsMouseVisible = false;
            graphics.ApplyChanges();

            _waveProcessor = new WaveProcessor(_enemyWaveStack);

            _gameArea = new Rectangle(
            (graphics.PreferredBackBufferWidth - 1300) / 2,
            (graphics.PreferredBackBufferHeight - 750) / 2,
            1300,
            750
            );
            _gameViewport = GraphicsDevice.Viewport;
            _gameViewport.Bounds = _gameArea;

            _uiViewport = GraphicsDevice.Viewport;
            _uiViewport.Bounds = new Rectangle(0, 0, 1920, 1080);

           var player_model = new PlayerModel(new Vector2(640, 600), new AttackPattern(
           shootInterval: 0.2f,
           bulletSpeed: 900f,
           bulletsPerShot: 8,
           true,
           strategy: new ZRadiusBulletStrategy(() => _inputHandler.GetDirectionAimPlayer(), Color.White)));
           player = new PlayerController(player_model, new PlayerView(player_model));

            _bulletPool = new OptimizedBulletPool();

            _spawnManager = new SpawnManager(
                rnd,
                _gameArea,
                _enemies,
                 _bonuses,
                _enemyWaveStack,
                player
            );

            _inputHandler = new InputHandler(
                player,
                this,
                _bulletPool,
                _gameArea
            );

            _spawnManager.InitializeWaveStack(_currentLevelData);

            _menuInputHandler = new MenuInputHandler(this);

            var bonusModel = new BonusModel(
                new AttackPattern(
               0.2f, 900f, 12, true,
               new ZRadiusBulletStrategy(_inputHandler.GetDirectionAimPlayer, Color.White)),
                Vector2.Zero,
                "空",
                "Пустота",
                Color.White,
                1
            );

            defaultBonus = new BonusController(bonusModel, new BonusView(bonusModel));

            base.Initialize();
            _uiManager._player = player;
        }

        protected override void Update(GameTime gameTime)
        {
            if (_currentState == GameState.Playing && !_gameplayManager.BattleStarted)
            {
                _uiManager.UpdatePreBattle(gameTime, Lvl, _inputHandler.IsSkipRequested);
            }

            if (_currentState == GameState.Menu)
            {
                _menuInputHandler.Update();
            }
            else if (_currentState == GameState.Pause)
            {
                _menuInputHandler.Update();
                base.Update(gameTime);
            }
            else if (_currentState == GameState.Victory)
            {
                if (Lvl < 2)
                {
                    Lvl++;
                    _gameplayManager.ResetGameState(Lvl);
                    _currentState = GameState.Playing;
                    _menuInputHandler.Update();
                }
                else
                {
                    _currentState = GameState.GameCompleted;
                }
            }
            else if (_currentState == GameState.GameCompleted)
            {
                Lvl = 1;
                _currentState = GameState.Menu;
            }
            else
            {
                _inputHandler.Update(gameTime);

                if (!_gameplayManager.BattleStarted && _inputHandler.IsSkipRequested)
                {
                    _gameplayManager.SkipPreBattle();
                    _inputHandler.IsSkipRequested = false;
                }

                _gameplayManager.Update(gameTime, _inputHandler);

                if (_gameplayManager.IsLevelCompleted())
                    _currentState = GameState.Victory;

                if (!_gameplayManager.IsPlayerAlive)
                {
                    _gameplayManager.ResetGameState(Lvl);
                    _currentState = GameState.Menu;
                }
                base.Update(gameTime);
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            if (_currentState == GameState.Menu)
            {
                _uiManager.DrawMenu(_selectedMenuItem, MenuItems, gameTime);
            }
            else if (_currentState == GameState.Pause)
            {
                _uiManager.DrawMenu(_selectedMenuItem, MenuItems, gameTime);
            }
            else
            {
                _uiManager.DrawGameUI(_gameplayManager.BattleStarted, _gameplayManager.Name, _gameplayManager.NameColor, Lvl);

                if (_gameplayManager.BattleStarted)
                {
                    _gameRenderer.Draw(player, _enemies, _bonuses, _bulletPool, _uiManager._japanSymbol);
                }
            }

            base.Draw(gameTime);
        }

        public void ResetAnimation()
        {
            _uiManager.ResetMenuAnimation();
        }

        public void ResetGameState(int lvl)
        {
            if (lvl == 1)
                _uiManager.ResetLevel1Intro();

            _gameplayManager.ResetGameState(lvl);
        }

    }
}