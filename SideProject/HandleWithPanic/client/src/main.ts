import "./style.css";
import { PanicDemoApp } from "./game";

document.querySelector<HTMLDivElement>("#app")!.innerHTML = `
  <div class="app-shell">
    <div class="world-layer">
      <div id="viewport"></div>

      <div id="game-hud" class="game-hud hidden">
        <div class="hud-top">
          <span id="objective-text">Escort the cargo to the delivery ring.</span>
          <span id="player-count">Couriers: 0 / 2</span>
        </div>
        <div class="crosshair"></div>
        <div class="hud-bottom">
          <div class="hud-bars">
            <div class="hud-bar-block">
              <div class="bar-header">
                <span>Fragility</span>
                <span id="fragility-value">100%</span>
              </div>
              <div class="bar-track"><div id="fragility-bar" class="bar-fill fragility"></div></div>
            </div>
            <div class="hud-bar-block">
              <div class="bar-header">
                <span>Stability</span>
                <span id="stability-value">100%</span>
              </div>
              <div class="bar-track"><div id="stability-bar" class="bar-fill stability"></div></div>
            </div>
          </div>
          <div class="hint-strip">
            <span><code>WASD</code> Move</span>
            <span><code>Mouse</code> Look</span>
            <span><code>F</code> Grab</span>
            <span><code>Q</code> Blanket</span>
            <span><code>E</code> Catapult</span>
            <span><code>R</code> Cart</span>
          </div>
        </div>
      </div>
    </div>

    <div id="event-banner" class="event-banner hidden"></div>

    <section id="screen-start" class="screen screen-start">
      <div class="panel hero-panel">
        <p class="eyebrow">Handle With Panic</p>
        <h1>废墟快递</h1>
        <p class="summary">2 人网页合作原型。先集结队伍，再锁定职业，从公共池分道具，最后完成裂隙协作运输。</p>
        <div class="hero-actions">
          <button id="start-game-button" type="button" class="primary-button">开始游戏</button>
        </div>
        <div class="hero-actions compact">
          <button id="solo-debug-button" type="button" class="secondary-button">单人调试模式</button>
        </div>
        <div class="join-row">
          <input id="room-code-input" type="text" maxlength="12" placeholder="输入房间码加入" />
          <button id="join-room-button" type="button" class="secondary-button">加入房间</button>
        </div>
        <p id="connection-status" class="status-line">尚未连接房间服务器。</p>
        <p class="status-line subtle">单人调试模式会创建仅限 1 人的测试房，并放宽大厅推进与裂隙接货规则。</p>
      </div>
    </section>

    <section id="screen-connecting" class="screen hidden">
      <div class="panel modal-panel">
        <p class="eyebrow">Connecting</p>
        <h2>正在连接服务器</h2>
        <p id="connecting-status" class="summary">正在创建或加入房间，请稍候...</p>
        <button id="cancel-connect-button" type="button" class="secondary-button">返回开始界面</button>
      </div>
    </section>

    <section id="screen-lobby" class="screen hidden">
      <div class="panel flow-panel">
        <div class="panel-header">
          <div>
            <p class="eyebrow">Lobby</p>
            <h2>联机大厅</h2>
          </div>
          <div class="room-meta">
            <div>
              <span class="meta-label">Room</span>
              <strong id="room-code">----</strong>
            </div>
            <div>
              <span class="meta-label">Host</span>
              <strong id="host-status">Waiting</strong>
            </div>
          </div>
        </div>

        <div class="stage-strip">
          <div class="stage-chip" data-stage-chip="lobby">大厅</div>
          <div class="stage-chip" data-stage-chip="role_select">角色</div>
          <div class="stage-chip" data-stage-chip="item_select">道具</div>
          <div class="stage-chip" data-stage-chip="playing">关卡</div>
        </div>

        <div id="lobby-player-list" class="player-grid"></div>

        <div class="selection-panel">
          <p class="section-title">职业预选</p>
          <div class="button-row">
            <button id="lobby-role-heavy" type="button" class="role-button">Heavy Lifter</button>
            <button id="lobby-role-runner" type="button" class="role-button">Runner</button>
          </div>
        </div>

        <div class="action-row">
          <button id="ready-button" type="button" class="primary-button">Ready Up</button>
          <button id="begin-role-select-button" type="button" class="secondary-button">进入角色选择</button>
        </div>
      </div>
    </section>

    <section id="screen-role" class="screen hidden">
      <div class="panel flow-panel">
        <div class="panel-header">
          <div>
            <p class="eyebrow">Role Select</p>
            <h2>角色选择</h2>
          </div>
          <button id="return-lobby-button" type="button" class="secondary-button header-button">返回大厅</button>
        </div>

        <div class="stage-strip">
          <div class="stage-chip" data-stage-chip="lobby">大厅</div>
          <div class="stage-chip" data-stage-chip="role_select">角色</div>
          <div class="stage-chip" data-stage-chip="item_select">道具</div>
          <div class="stage-chip" data-stage-chip="playing">关卡</div>
        </div>

        <div id="role-player-list" class="player-grid"></div>

        <div class="card-grid">
          <button id="role-heavy" type="button" class="choice-card">
            <span class="choice-title">Heavy Lifter</span>
            <span class="choice-copy">更重、更稳、更适合扛货，但不能被投石车发射过裂隙。</span>
          </button>
          <button id="role-runner" type="button" class="choice-card">
            <span class="choice-title">Runner</span>
            <span class="choice-copy">更轻、更快、更适合先行跨隙，并负责毛毯接货。</span>
          </button>
        </div>

        <div class="action-row">
          <button id="role-lock-button" type="button" class="primary-button">锁定角色</button>
          <button id="role-advance-button" type="button" class="secondary-button">进入道具选取</button>
        </div>
      </div>
    </section>

    <section id="screen-items" class="screen hidden">
      <div class="panel flow-panel">
        <div class="panel-header">
          <div>
            <p class="eyebrow">Item Select</p>
            <h2>道具选取</h2>
          </div>
          <button id="return-role-select-button" type="button" class="secondary-button header-button">返回角色选择</button>
        </div>

        <div class="stage-strip">
          <div class="stage-chip" data-stage-chip="lobby">大厅</div>
          <div class="stage-chip" data-stage-chip="role_select">角色</div>
          <div class="stage-chip" data-stage-chip="item_select">道具</div>
          <div class="stage-chip" data-stage-chip="playing">关卡</div>
        </div>

        <div id="item-player-list" class="player-grid"></div>

        <div class="item-grid">
          <button id="item-blanket" type="button" class="choice-card item-card">
            <span class="choice-title">Blanket</span>
            <span class="choice-copy">负责裂隙接货，是当前关卡的关键协作道具。</span>
            <strong id="item-count-blanket">x1</strong>
          </button>
          <button id="item-cart" type="button" class="choice-card item-card">
            <span class="choice-title">Fold Cart</span>
            <span class="choice-copy">允许玩家部署折叠车，提高平地运输效率。</span>
            <strong id="item-count-cart">x1</strong>
          </button>
          <button id="item-stabilizer" type="button" class="choice-card item-card">
            <span class="choice-title">Stabilizer Strap</span>
            <span class="choice-copy">降低搬运过程中的稳定度损耗。</span>
            <strong id="item-count-stabilizer">x2</strong>
          </button>
          <button id="item-buffer" type="button" class="choice-card item-card">
            <span class="choice-title">Impact Pad</span>
            <span class="choice-copy">减少一次掉入裂隙时的货物损伤。</span>
            <strong id="item-count-buffer">x2</strong>
          </button>
        </div>

        <div class="selection-panel">
          <p class="section-title">我的携带道具</p>
          <p id="selected-items-text" class="summary">尚未选择道具。</p>
        </div>

        <div class="action-row">
          <button id="item-confirm-button" type="button" class="primary-button">确认道具</button>
          <button id="start-run-button" type="button" class="secondary-button">进入游戏</button>
        </div>
      </div>
    </section>

    <section id="screen-result" class="screen hidden">
      <div class="panel modal-panel">
        <p id="result-phase-label" class="eyebrow">Run Result</p>
        <h2 id="phase-label">Completed</h2>
        <p id="announcement" class="summary">Waiting for room state...</p>
        <button id="restart-button" type="button" class="primary-button">返回大厅</button>
      </div>
    </section>
  </div>
`;

new PanicDemoApp({
  viewport: document.querySelector<HTMLDivElement>("#viewport")!,
  startScreen: document.querySelector<HTMLElement>("#screen-start")!,
  connectingScreen: document.querySelector<HTMLElement>("#screen-connecting")!,
  lobbyScreen: document.querySelector<HTMLElement>("#screen-lobby")!,
  roleScreen: document.querySelector<HTMLElement>("#screen-role")!,
  itemScreen: document.querySelector<HTMLElement>("#screen-items")!,
  resultScreen: document.querySelector<HTMLElement>("#screen-result")!,
  gameHud: document.querySelector<HTMLElement>("#game-hud")!,
  eventBanner: document.querySelector<HTMLElement>("#event-banner")!,
  startGameButton: document.querySelector<HTMLButtonElement>("#start-game-button")!,
  soloDebugButton: document.querySelector<HTMLButtonElement>("#solo-debug-button")!,
  joinRoomButton: document.querySelector<HTMLButtonElement>("#join-room-button")!,
  cancelConnectButton: document.querySelector<HTMLButtonElement>("#cancel-connect-button")!,
  roomCodeInput: document.querySelector<HTMLInputElement>("#room-code-input")!,
  connectButton: document.querySelector<HTMLButtonElement>("#start-game-button")!,
  roomCode: document.querySelector<HTMLElement>("#room-code")!,
  hostStatus: document.querySelector<HTMLElement>("#host-status")!,
  connectingStatus: document.querySelector<HTMLElement>("#connecting-status")!,
  resultPhaseLabel: document.querySelector<HTMLElement>("#result-phase-label")!,
  stageChips: Array.from(document.querySelectorAll<HTMLElement>("[data-stage-chip]")),
  lobbyPlayerList: document.querySelector<HTMLDivElement>("#lobby-player-list")!,
  rolePlayerList: document.querySelector<HTMLDivElement>("#role-player-list")!,
  itemPlayerList: document.querySelector<HTMLDivElement>("#item-player-list")!,
  lobbyRoleHeavyButton: document.querySelector<HTMLButtonElement>("#lobby-role-heavy")!,
  lobbyRoleRunnerButton: document.querySelector<HTMLButtonElement>("#lobby-role-runner")!,
  roleHeavyButton: document.querySelector<HTMLButtonElement>("#role-heavy")!,
  roleRunnerButton: document.querySelector<HTMLButtonElement>("#role-runner")!,
  readyButton: document.querySelector<HTMLButtonElement>("#ready-button")!,
  beginRoleSelectButton: document.querySelector<HTMLButtonElement>("#begin-role-select-button")!,
  roleLockButton: document.querySelector<HTMLButtonElement>("#role-lock-button")!,
  roleAdvanceButton: document.querySelector<HTMLButtonElement>("#role-advance-button")!,
  returnLobbyButton: document.querySelector<HTMLButtonElement>("#return-lobby-button")!,
  itemBlanketButton: document.querySelector<HTMLButtonElement>("#item-blanket")!,
  itemCartButton: document.querySelector<HTMLButtonElement>("#item-cart")!,
  itemStabilizerButton: document.querySelector<HTMLButtonElement>("#item-stabilizer")!,
  itemBufferButton: document.querySelector<HTMLButtonElement>("#item-buffer")!,
  itemCountBlanket: document.querySelector<HTMLElement>("#item-count-blanket")!,
  itemCountCart: document.querySelector<HTMLElement>("#item-count-cart")!,
  itemCountStabilizer: document.querySelector<HTMLElement>("#item-count-stabilizer")!,
  itemCountBuffer: document.querySelector<HTMLElement>("#item-count-buffer")!,
  selectedItemsText: document.querySelector<HTMLParagraphElement>("#selected-items-text")!,
  itemConfirmButton: document.querySelector<HTMLButtonElement>("#item-confirm-button")!,
  startRunButton: document.querySelector<HTMLButtonElement>("#start-run-button")!,
  returnRoleSelectButton: document.querySelector<HTMLButtonElement>("#return-role-select-button")!,
  restartButton: document.querySelector<HTMLButtonElement>("#restart-button")!,
  connectionStatus: document.querySelector<HTMLParagraphElement>("#connection-status")!,
  phaseLabel: document.querySelector<HTMLParagraphElement>("#phase-label")!,
  announcement: document.querySelector<HTMLParagraphElement>("#announcement")!,
  fragilityBar: document.querySelector<HTMLDivElement>("#fragility-bar")!,
  fragilityValue: document.querySelector<HTMLSpanElement>("#fragility-value")!,
  stabilityBar: document.querySelector<HTMLDivElement>("#stability-bar")!,
  stabilityValue: document.querySelector<HTMLSpanElement>("#stability-value")!,
  objectiveText: document.querySelector<HTMLSpanElement>("#objective-text")!,
  playerCount: document.querySelector<HTMLSpanElement>("#player-count")!,
});
