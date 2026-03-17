import { Room, Client } from "colyseus";
import { Schema, type, MapSchema } from "@colyseus/schema";
import RAPIER from "@dimforge/rapier3d-compat";

type RoleType = "heavy" | "runner";
type ItemType = "blanket" | "cart" | "stabilizer" | "buffer";
type PhaseType = "lobby" | "role_select" | "item_select" | "playing" | "completed" | "failed";

interface PlayerUpdateMessage {
  position: { x: number; y: number; z: number };
  yaw: number;
  blanketActive: boolean;
}

class Vec3State extends Schema {
  @type("number") x = 0;
  @type("number") y = 0;
  @type("number") z = 0;

  set(x: number, y: number, z: number): void {
    this.x = x;
    this.y = y;
    this.z = z;
  }
}

class PlayerState extends Schema {
  @type("string") sessionId = "";
  @type("string") name = "Courier";
  @type("string") role: RoleType = "heavy";
  @type("boolean") ready = false;
  @type("boolean") roleLocked = false;
  @type("boolean") connected = true;
  @type("boolean") blanketActive = false;
  @type("string") itemSlotA = "";
  @type("string") itemSlotB = "";
  @type("boolean") itemConfirmed = false;
  @type("number") yaw = 0;
  @type("number") catapultCooldown = 0;
  @type(Vec3State) position = new Vec3State();
}

class ItemPoolState extends Schema {
  @type("number") blanket = 1;
  @type("number") cart = 1;
  @type("number") stabilizer = 2;
  @type("number") buffer = 2;
}

class CargoState extends Schema {
  @type(Vec3State) position = new Vec3State();
  @type(Vec3State) velocity = new Vec3State();
  @type("number") fragility = 100;
  @type("number") stability = 100;
  @type("boolean") isCarried = false;
  @type("string") carrierId = "";
  @type("boolean") cartLatched = false;
  @type("boolean") panic = false;
  @type("boolean") airborne = false;
}

class DemoState extends Schema {
  @type({ map: PlayerState }) players = new MapSchema<PlayerState>();
  @type(CargoState) cargo = new CargoState();
  @type("string") phase: PhaseType = "lobby";
  @type("number") elapsedTime = 0;
  @type("string") announcement = "Pick a role and ready up.";
  @type("number") connectedCount = 0;
  @type("number") maxPlayers = 2;
  @type("string") hostSessionId = "";
  @type("boolean") soloDebug = false;
  @type(ItemPoolState) itemPool = new ItemPoolState();
}

const PLAYER_HEIGHT = 1.7;
const INTERACT_DISTANCE = 2.4;
const CARGO_HEIGHT = 1.2;
const PLAYER_LEFT_SPAWN = { x: -1.5, y: PLAYER_HEIGHT, z: 3.5 };
const PLAYER_RIGHT_SPAWN = { x: 0, y: PLAYER_HEIGHT, z: 22.5 };
const CARGO_LEFT_SPAWN = { x: 0, y: CARGO_HEIGHT, z: 4.8 };
const CARGO_LAUNCH_RESET = { x: 0, y: CARGO_HEIGHT, z: 7.6 };
const CATAPULT_POSITION = { x: 0, y: 0, z: 7.6 };
const CATCH_ZONE = { x: 0, y: 1.4, z: 21.5, radius: 3 };
const DELIVERY_ZONE_Z = 28;
const GAP_START_Z = 10;
const GAP_END_Z = 18.5;
const ITEM_POOL_TOTAL: Record<ItemType, number> = {
  blanket: 1,
  cart: 1,
  stabilizer: 2,
  buffer: 2,
};

export class PanicRoom extends Room<{ state: DemoState }> {
  maxClients = 2;

  private world!: RAPIER.World;
  private cargoBody!: RAPIER.RigidBody;
  private panicImpulseTimer = 0.8;
  private simulationTime = 0;

  onCreate(options?: { soloDebug?: boolean }): void {
    this.setState(new DemoState());
    this.state.soloDebug = Boolean(options?.soloDebug);
    if (this.state.soloDebug) {
      this.maxClients = 1;
      this.state.maxPlayers = 1;
      this.state.announcement = "Solo debug enabled. One courier can advance alone.";
    }
    this.setupWorld();
    this.resetCargo();
    this.refreshItemPool();

    this.onMessage("set_role", (client, payload: { role?: RoleType }) => {
      const player = this.state.players.get(client.sessionId);
      if (!player || (this.state.phase !== "lobby" && this.state.phase !== "role_select")) {
        return;
      }

      if (this.state.phase === "role_select" && player.roleLocked) {
        return;
      }

      if (payload.role === "heavy" || payload.role === "runner") {
        player.role = payload.role;
        if (this.state.phase === "lobby") {
          player.ready = false;
        }
        if (this.state.phase === "role_select") {
          player.roleLocked = false;
        }
        this.refreshAnnouncement();
      }
    });

    this.onMessage("set_ready", (client, payload: { ready?: boolean }) => {
      const player = this.state.players.get(client.sessionId);
      if (!player || this.state.phase !== "lobby") {
        return;
      }

      player.ready = Boolean(payload.ready);
      this.refreshAnnouncement();
    });

    this.onMessage("host_begin_role_select", (client) => {
      if (!this.isHost(client.sessionId) || this.state.phase !== "lobby") {
        return;
      }

      if (!this.canEnterRoleSelect()) {
        this.state.announcement = this.state.soloDebug
          ? "Solo debug still needs your courier to Ready before role select."
          : "Need two ready couriers before opening role select.";
        return;
      }

      this.beginRoleSelect();
    });

    this.onMessage("lock_role", (client, payload: { locked?: boolean }) => {
      const player = this.state.players.get(client.sessionId);
      if (!player || this.state.phase !== "role_select") {
        return;
      }

      const shouldLock = Boolean(payload.locked);
      if (shouldLock) {
        const conflictingPlayer = this.findLockedPlayerWithRole(player.role, client.sessionId);
        if (conflictingPlayer) {
          this.state.announcement = `${player.role === "heavy" ? "Heavy Lifter" : "Runner"} is already locked by ${conflictingPlayer.name}.`;
          return;
        }
      }

      player.roleLocked = shouldLock;
      this.refreshAnnouncement();
    });

    this.onMessage("host_begin_item_select", (client) => {
      if (!this.isHost(client.sessionId) || this.state.phase !== "role_select") {
        return;
      }

      if (!this.canEnterItemSelect()) {
        this.state.announcement = this.state.soloDebug
          ? "Lock your debug role before choosing items."
          : "Lock one Heavy Lifter and one Runner before choosing items.";
        return;
      }

      this.beginItemSelect();
    });

    this.onMessage("toggle_item", (client, payload: { item?: ItemType }) => {
      const player = this.state.players.get(client.sessionId);
      if (!player || this.state.phase !== "item_select") {
        return;
      }

      const item = payload.item;
      if (!item || !this.isValidItemType(item)) {
        return;
      }

      player.itemConfirmed = false;

      if (player.itemSlotA === item) {
        player.itemSlotA = player.itemSlotB;
        player.itemSlotB = "";
        this.refreshItemPool();
        this.refreshAnnouncement();
        return;
      }

      if (player.itemSlotB === item) {
        player.itemSlotB = "";
        this.refreshItemPool();
        this.refreshAnnouncement();
        return;
      }

      if (this.getPlayerItems(player).length >= 2) {
        this.state.announcement = `${player.name} can only carry two items.`;
        return;
      }

      if (this.getRemainingItemStock(item) <= 0) {
        this.state.announcement = `${this.getItemLabel(item)} is already taken.`;
        return;
      }

      if (!player.itemSlotA) {
        player.itemSlotA = item;
      } else {
        player.itemSlotB = item;
      }

      this.refreshItemPool();
      this.refreshAnnouncement();
    });

    this.onMessage("confirm_items", (client, payload: { confirmed?: boolean }) => {
      const player = this.state.players.get(client.sessionId);
      if (!player || this.state.phase !== "item_select") {
        return;
      }

      const confirmed = Boolean(payload.confirmed);
      if (confirmed && !this.canPlayerConfirmItems(player)) {
        this.state.announcement = `${player.name} must choose one or two items before confirming.`;
        return;
      }

      player.itemConfirmed = confirmed;
      this.refreshAnnouncement();
    });

    this.onMessage("host_return_lobby", (client) => {
      if (!this.isHost(client.sessionId)) {
        return;
      }

      this.returnToLobby("Host returned the crew to the lobby.");
    });

    this.onMessage("host_return_role_select", (client) => {
      if (!this.isHost(client.sessionId) || this.state.phase !== "item_select") {
        return;
      }

      this.returnToRoleSelect();
    });

    this.onMessage("host_start_match", (client) => {
      if (!this.isHost(client.sessionId) || this.state.phase !== "item_select") {
        return;
      }

      if (!this.canStartMatch()) {
        this.state.announcement = this.state.soloDebug
          ? "Solo debug needs your courier to confirm 1-2 items before starting."
          : "All couriers must confirm 1-2 items, and the team needs a blanket.";
        return;
      }

      this.startMatch();
    });

    this.onMessage("player_update", (client, payload: PlayerUpdateMessage) => {
      const player = this.state.players.get(client.sessionId);
      if (!player || this.state.phase === "completed" || this.state.phase === "failed") {
        return;
      }

      player.position.set(payload.position.x, payload.position.y, payload.position.z);
      player.yaw = payload.yaw;
      player.blanketActive = payload.blanketActive && this.playerHasItem(player, "blanket");
    });

    this.onMessage("interact", (client) => {
      if (this.state.phase !== "playing") {
        return;
      }

      const player = this.state.players.get(client.sessionId);
      if (!player) {
        return;
      }

      if (!this.playerHasItem(player, "cart")) {
        this.state.announcement = `${player.name} needs the Fold Cart item to deploy the cart.`;
        return;
      }

      if (this.state.cargo.carrierId === client.sessionId) {
        this.releaseCargo();
        this.state.announcement = `${player.name} put the cargo down.`;
        return;
      }

      if (this.state.cargo.isCarried) {
        return;
      }

      const distance = this.distance3(player.position, this.cargoBody.translation());
      if (distance <= INTERACT_DISTANCE) {
        this.state.cargo.carrierId = client.sessionId;
        this.state.cargo.isCarried = true;
        this.state.cargo.cartLatched = false;
        this.state.cargo.airborne = false;
        this.state.announcement = `${player.name} grabbed the cargo.`;
      }
    });

    this.onMessage("toggle_cart", (client) => {
      if (this.state.phase !== "playing" || this.state.cargo.isCarried) {
        return;
      }

      const player = this.state.players.get(client.sessionId);
      if (!player) {
        return;
      }

      const distance = this.distance3(player.position, this.cargoBody.translation());
      if (distance <= INTERACT_DISTANCE) {
        this.state.cargo.cartLatched = !this.state.cargo.cartLatched;
        this.state.announcement = this.state.cargo.cartLatched
          ? "Cargo locked onto the fold cart."
          : "Fold cart released.";
      }
    });

    this.onMessage("use_catapult", (client) => {
      if (this.state.phase !== "playing") {
        return;
      }

      const player = this.state.players.get(client.sessionId);
      if (!player || player.catapultCooldown > 0 || !this.isNearCatapult(player.position)) {
        return;
      }

      player.catapultCooldown = 0.85;

      if (this.state.soloDebug && this.state.cargo.carrierId === client.sessionId) {
        this.releaseCargo();
        this.cargoBody.setTranslation(CARGO_LAUNCH_RESET, true);
        this.launchCargo();
        player.position.set(PLAYER_RIGHT_SPAWN.x, PLAYER_RIGHT_SPAWN.y, PLAYER_RIGHT_SPAWN.z);
        this.state.announcement = `${player.name} triggered the solo debug launch and crossed with the cargo route.`;
        return;
      }

      if (this.state.soloDebug && !this.state.cargo.isCarried) {
        player.position.set(PLAYER_RIGHT_SPAWN.x, PLAYER_RIGHT_SPAWN.y, PLAYER_RIGHT_SPAWN.z);
        this.state.announcement = `${player.name} used the solo debug catapult crossing.`;
        return;
      }

      if (player.role === "runner" && !this.state.cargo.isCarried) {
        player.position.set(PLAYER_RIGHT_SPAWN.x, PLAYER_RIGHT_SPAWN.y, PLAYER_RIGHT_SPAWN.z);
        this.state.announcement = `${player.name} was launched across the rift.`;
        return;
      }

      if (player.role === "heavy" && !this.isCargoNearCatapult()) {
        this.state.announcement = "Heavy Lifter is too heavy for this catapult.";
        return;
      }

      if (this.state.cargo.carrierId === client.sessionId) {
        this.releaseCargo();
        this.cargoBody.setTranslation(CARGO_LAUNCH_RESET, true);
      }

      if (!this.isCargoNearCatapult()) {
        this.state.announcement = "Move the cargo onto the catapult first.";
        return;
      }

      this.launchCargo();
      this.state.announcement = `${player.name} launched the cargo. Runner, catch it with the blanket!`;
    });

    this.onMessage("restart", () => {
      if (this.state.phase === "completed" || this.state.phase === "failed") {
        this.returnToLobby("Run reset. Pick roles, ready up, then prepare the crew again.");
      }
    });

    this.setSimulationInterval((deltaTimeMs) => {
      this.fixedUpdate(deltaTimeMs / 1000);
    }, 1000 / 60);
  }

  onJoin(client: Client, options: { name?: string } = {}): void {
    const player = new PlayerState();
    player.sessionId = client.sessionId;
    player.name = options.name?.trim() || `Courier ${this.clients.length}`;
    player.position.set(
      this.clients.length === 1 ? PLAYER_LEFT_SPAWN.x - 1.2 : PLAYER_LEFT_SPAWN.x + 1.2,
      PLAYER_LEFT_SPAWN.y,
      PLAYER_LEFT_SPAWN.z,
    );
    this.state.players.set(client.sessionId, player);
    if (!this.state.hostSessionId) {
      this.state.hostSessionId = client.sessionId;
    }
    this.state.connectedCount = this.state.players.size;
    this.refreshAnnouncement();
  }

  onLeave(client: Client): void {
    if (this.state.cargo.carrierId === client.sessionId) {
      this.releaseCargo();
    }

    this.state.players.delete(client.sessionId);
    this.state.connectedCount = this.state.players.size;
    if (this.state.hostSessionId === client.sessionId) {
      this.state.hostSessionId = this.state.players.keys().next().value ?? "";
    }

    if (this.state.players.size === 0) {
      this.returnToLobby("Waiting for couriers.");
      return;
    }

    if (this.state.phase === "playing") {
      this.state.announcement = "A courier dropped. Keep the route alive.";
    } else if (this.state.phase === "role_select" || this.state.phase === "item_select") {
      this.returnToLobby("A courier left during preparation. Returning to lobby.");
    } else {
      this.refreshAnnouncement();
    }
  }

  private setupWorld(): void {
    this.world = new RAPIER.World({ x: 0, y: -9.81, z: 0 });

    this.createFloor(0, -0.4, 4.5, 6.2, 0.4, 5.5);
    this.createFloor(0, -0.4, 24.5, 6.2, 0.4, 6.5);

    this.createWall(-6.3, 0.9, 4.5, 0.2, 1.8, 5.5);
    this.createWall(6.3, 0.9, 4.5, 0.2, 1.8, 5.5);
    this.createWall(-6.3, 0.9, 24.5, 0.2, 1.8, 6.5);
    this.createWall(6.3, 0.9, 24.5, 0.2, 1.8, 6.5);

    const cargoBodyDesc = RAPIER.RigidBodyDesc.dynamic()
      .setTranslation(CARGO_LEFT_SPAWN.x, CARGO_LEFT_SPAWN.y, CARGO_LEFT_SPAWN.z)
      .setLinearDamping(1.1)
      .setAngularDamping(1.8)
      .setCanSleep(false)
      .setCcdEnabled(true);
    this.cargoBody = this.world.createRigidBody(cargoBodyDesc);

    const colliderDesc = RAPIER.ColliderDesc.cuboid(0.55, 0.55, 0.55)
      .setDensity(0.7)
      .setRestitution(0.05)
      .setFriction(1.0);
    this.world.createCollider(colliderDesc, this.cargoBody);
  }

  private createFloor(x: number, y: number, z: number, hx: number, hy: number, hz: number): void {
    const body = this.world.createRigidBody(RAPIER.RigidBodyDesc.fixed().setTranslation(x, y, z));
    this.world.createCollider(RAPIER.ColliderDesc.cuboid(hx, hy, hz), body);
  }

  private createWall(x: number, y: number, z: number, hx: number, hy: number, hz: number): void {
    const body = this.world.createRigidBody(RAPIER.RigidBodyDesc.fixed().setTranslation(x, y, z));
    this.world.createCollider(RAPIER.ColliderDesc.cuboid(hx, hy, hz), body);
  }

  private startMatch(): void {
    this.preparePlayersForMatch();
    this.resetCargo();
    this.state.phase = "playing";
    this.state.elapsedTime = 0;
    this.state.announcement = "Launch the Runner across the rift, then catapult the cargo.";
  }

  private fixedUpdate(deltaTime: number): void {
    this.simulationTime += deltaTime;

    for (const player of this.state.players.values()) {
      player.catapultCooldown = Math.max(0, player.catapultCooldown - deltaTime);
      if (this.isInsideRift(player.position)) {
        this.respawnPlayer(player);
      }
    }

    if (this.state.phase !== "playing") {
      this.syncCargoState();
      return;
    }

    this.state.elapsedTime += deltaTime;
    this.simulateCarriedCargo();
    this.world.step();
    this.simulateCargoStatus(deltaTime);
    this.handleCargoCatch();
    this.handleCargoLoss();
    this.syncCargoState();
    this.checkCompletion();
  }

  private simulateCarriedCargo(): void {
    const carrier = this.state.players.get(this.state.cargo.carrierId);
    if (!carrier) {
      this.releaseCargo();
      return;
    }

    if (!this.state.cargo.isCarried) {
      return;
    }

    const forward = {
      x: -Math.sin(carrier.yaw),
      y: 0,
      z: -Math.cos(carrier.yaw),
    };
    const carryDistance = carrier.role === "heavy" ? 1.35 : 1.15;
    const target = {
      x: carrier.position.x + forward.x * carryDistance,
      y: carrier.role === "heavy" ? 1.15 : 1.25,
      z: carrier.position.z + forward.z * carryDistance,
    };

    const current = this.cargoBody.translation();
    this.cargoBody.setLinvel(
      {
        x: (target.x - current.x) * (carrier.role === "heavy" ? 14 : 11),
        y: (target.y - current.y) * 10,
        z: (target.z - current.z) * (carrier.role === "heavy" ? 14 : 11),
      },
      true,
    );
    this.cargoBody.setAngvel({ x: 0, y: 0, z: 0 }, true);
    this.state.cargo.airborne = false;
  }

  private simulateCargoStatus(deltaTime: number): void {
    const cargoPosition = this.cargoBody.translation();
    const cargoVelocity = this.cargoBody.linvel();
    const speed = Math.hypot(cargoVelocity.x, cargoVelocity.y, cargoVelocity.z);
    const inRiftAir = cargoPosition.z > GAP_START_Z && cargoPosition.z < GAP_END_Z && cargoPosition.y > 0.2;

    let stabilityDrain = 0.35 * deltaTime;
    if (!this.state.cargo.isCarried && speed > 2.8) {
      stabilityDrain += (speed - 2.8) * 0.75 * deltaTime;
      this.state.cargo.fragility = this.clamp(
        this.state.cargo.fragility - Math.max(0, speed - 4.2) * 6 * deltaTime,
        0,
        100,
      );
    }

    if (inRiftAir) {
      stabilityDrain += 2.2 * deltaTime;
      this.state.cargo.airborne = true;
    }

    const carrier = this.state.players.get(this.state.cargo.carrierId);
    if (carrier && this.playerHasItem(carrier, "stabilizer")) {
      stabilityDrain *= 0.72;
    }

    if (this.state.cargo.cartLatched && !this.state.cargo.isCarried) {
      this.state.cargo.fragility = Math.min(100, this.state.cargo.fragility + deltaTime * 0.8);
    }

    if (this.hasBlanketCarrier()) {
      this.state.cargo.fragility = Math.min(100, this.state.cargo.fragility + deltaTime * 1.8);
    }

    this.state.cargo.stability = this.clamp(this.state.cargo.stability - stabilityDrain, 0, 100);
    this.state.cargo.panic = this.state.cargo.stability < 20;

    if (this.state.cargo.panic) {
      this.panicImpulseTimer -= deltaTime;
      if (this.panicImpulseTimer <= 0) {
        this.panicImpulseTimer = 0.7;
        this.cargoBody.applyImpulse(
          {
            x: (Math.random() - 0.5) * 1.4,
            y: 1.8,
            z: (Math.random() - 0.5) * 1.4,
          },
          true,
        );
        this.state.announcement = "The emotional cargo is panicking mid-air!";
      }
    } else {
      this.panicImpulseTimer = 0.7;
    }

    if (this.state.cargo.fragility <= 0) {
      this.state.phase = "failed";
      this.releaseCargo();
      this.state.announcement = "The cargo shattered in the rift. Reset and try again.";
    }
  }

  private handleCargoCatch(): void {
    if (this.state.soloDebug && this.state.cargo.airborne && !this.state.cargo.isCarried) {
      const cargoPosition = this.cargoBody.translation();
      const cargoVelocity = this.cargoBody.linvel();
      if (cargoVelocity.y <= 0 && cargoPosition.z >= GAP_END_Z) {
        this.cargoBody.setTranslation({ x: 0, y: CARGO_HEIGHT, z: 22.8 }, true);
        this.cargoBody.setLinvel({ x: 0, y: 0, z: 0 }, true);
        this.cargoBody.setAngvel({ x: 0, y: 0, z: 0 }, true);
        this.state.cargo.airborne = false;
        this.state.cargo.fragility = Math.min(100, this.state.cargo.fragility + 6);
        this.state.cargo.stability = Math.min(100, this.state.cargo.stability + 10);
        this.state.announcement = "Solo debug auto-catch placed the cargo on the far side.";
        return;
      }
    }

    if (!this.state.cargo.airborne || this.state.cargo.isCarried) {
      return;
    }

    const cargoPosition = this.cargoBody.translation();
    const cargoVelocity = this.cargoBody.linvel();
    if (cargoVelocity.y > 0 || cargoPosition.z < GAP_END_Z) {
      return;
    }

    for (const player of this.state.players.values()) {
      if (player.role !== "runner" || !player.blanketActive) {
        continue;
      }

      const distance = this.distance3(player.position, cargoPosition);
      if (distance > CATCH_ZONE.radius || player.position.z < GAP_END_Z) {
        continue;
      }

      const target = {
        x: player.position.x,
        y: CARGO_HEIGHT,
        z: player.position.z - 1.2,
      };
      this.cargoBody.setTranslation(target, true);
      this.cargoBody.setLinvel({ x: 0, y: 0, z: 0 }, true);
      this.cargoBody.setAngvel({ x: 0, y: 0, z: 0 }, true);
      this.state.cargo.airborne = false;
      this.state.cargo.fragility = Math.min(100, this.state.cargo.fragility + 8);
      this.state.cargo.stability = Math.min(100, this.state.cargo.stability + 12);
      this.state.announcement = `${player.name} caught the cargo with the blanket.`;
      return;
    }
  }

  private handleCargoLoss(): void {
    const cargoPosition = this.cargoBody.translation();
    if (cargoPosition.y > -3.5) {
      return;
    }

    const hasImpactPad = this.teamHasItem("buffer");
    const fragilityPenalty = hasImpactPad ? 10 : 16;
    const stabilityPenalty = hasImpactPad ? 14 : 22;
    this.state.cargo.fragility = this.clamp(this.state.cargo.fragility - fragilityPenalty, 0, 100);
    this.state.cargo.stability = this.clamp(this.state.cargo.stability - stabilityPenalty, 0, 100);
    this.releaseCargo();
    this.state.cargo.airborne = false;
    this.cargoBody.setTranslation(CARGO_LAUNCH_RESET, true);
    this.cargoBody.setLinvel({ x: 0, y: 0, z: 0 }, true);
    this.cargoBody.setAngvel({ x: 0, y: 0, z: 0 }, true);

    if (this.state.cargo.fragility <= 0) {
      this.state.phase = "failed";
      this.state.announcement = "The cargo broke after too many missed catches.";
      return;
    }

    this.state.announcement =
      "The cargo fell into the rift. It lost condition and reset to the launch side.";
  }

  private checkCompletion(): void {
    const cargoPosition = this.cargoBody.translation();
    if (cargoPosition.z < DELIVERY_ZONE_Z || Math.abs(cargoPosition.x) > 2.7 || cargoPosition.y < -0.5) {
      return;
    }

    this.state.phase = "completed";
    this.releaseCargo();
    this.state.announcement =
      this.state.cargo.fragility > 70 && this.state.cargo.stability > 55
        ? "Clean delivery. The client signs immediately."
        : "Rough landing, but the cargo still made it.";
  }

  private launchCargo(): void {
    this.state.cargo.airborne = true;
    this.state.cargo.cartLatched = false;
    this.cargoBody.setTranslation(CARGO_LAUNCH_RESET, true);
    this.cargoBody.setLinvel({ x: 0, y: 8.2, z: 14.6 }, true);
    this.cargoBody.setAngvel({ x: 1.2, y: 0.5, z: 0.4 }, true);
  }

  private resetPlayers(): void {
    let index = 0;
    for (const player of this.state.players.values()) {
      player.ready = false;
      player.roleLocked = false;
      player.itemConfirmed = false;
      player.itemSlotA = "";
      player.itemSlotB = "";
      player.blanketActive = false;
      player.catapultCooldown = 0;
      player.position.set(index === 0 ? -1.4 : 1.4, PLAYER_HEIGHT, PLAYER_LEFT_SPAWN.z);
      player.yaw = 0;
      index += 1;
    }

    this.refreshItemPool();
  }

  private preparePlayersForMatch(): void {
    let index = 0;
    for (const player of this.state.players.values()) {
      player.ready = false;
      player.blanketActive = false;
      player.catapultCooldown = 0;
      player.position.set(index === 0 ? -1.4 : 1.4, PLAYER_HEIGHT, PLAYER_LEFT_SPAWN.z);
      player.yaw = 0;
      index += 1;
    }
  }

  private resetCargo(): void {
    this.releaseCargo();
    this.simulationTime = 0;
    this.state.cargo.fragility = 100;
    this.state.cargo.stability = 100;
    this.state.cargo.cartLatched = false;
    this.state.cargo.panic = false;
    this.state.cargo.airborne = false;
    this.cargoBody.setTranslation(CARGO_LEFT_SPAWN, true);
    this.cargoBody.setLinvel({ x: 0, y: 0, z: 0 }, true);
    this.cargoBody.setAngvel({ x: 0, y: 0, z: 0 }, true);
    this.syncCargoState();
  }

  private respawnPlayer(player: PlayerState): void {
    player.position.set(PLAYER_LEFT_SPAWN.x, PLAYER_LEFT_SPAWN.y, PLAYER_LEFT_SPAWN.z);
    player.blanketActive = false;
    this.state.announcement = `${player.name} fell into the rift and respawned on the launch side.`;
  }

  private releaseCargo(): void {
    this.state.cargo.isCarried = false;
    this.state.cargo.carrierId = "";
  }

  private syncCargoState(): void {
    const translation = this.cargoBody.translation();
    const velocity = this.cargoBody.linvel();
    this.state.cargo.position.set(translation.x, translation.y, translation.z);
    this.state.cargo.velocity.set(velocity.x, velocity.y, velocity.z);
  }

  private isHost(sessionId: string): boolean {
    return this.state.hostSessionId === sessionId;
  }

  private canEnterRoleSelect(): boolean {
    return this.state.players.size >= (this.state.soloDebug ? 1 : 2) && this.areAllPlayersReady();
  }

  private canEnterItemSelect(): boolean {
    return this.areAllRolesLocked() && (this.state.soloDebug || this.hasRequiredRoleComposition());
  }

  private canStartMatch(): boolean {
    const requiredPlayers = this.state.soloDebug ? 1 : 2;
    if (this.state.players.size < requiredPlayers || !this.areAllItemsConfirmed()) {
      return false;
    }

    if (!this.state.soloDebug && !this.teamHasItem("blanket")) {
      return false;
    }

    for (const player of this.state.players.values()) {
      if (!this.canPlayerConfirmItems(player)) {
        return false;
      }
    }

    return true;
  }

  private canPlayerConfirmItems(player: PlayerState): boolean {
    const itemCount = this.getPlayerItems(player).length;
    return itemCount >= 1 && itemCount <= 2;
  }

  private areAllPlayersReady(): boolean {
    if (this.state.players.size < (this.state.soloDebug ? 1 : 2)) {
      return false;
    }

    for (const player of this.state.players.values()) {
      if (!player.ready) {
        return false;
      }
    }

    return true;
  }

  private areAllRolesLocked(): boolean {
    if (this.state.players.size < (this.state.soloDebug ? 1 : 2)) {
      return false;
    }

    for (const player of this.state.players.values()) {
      if (!player.roleLocked) {
        return false;
      }
    }

    return true;
  }

  private areAllItemsConfirmed(): boolean {
    if (this.state.players.size < (this.state.soloDebug ? 1 : 2)) {
      return false;
    }

    for (const player of this.state.players.values()) {
      if (!player.itemConfirmed) {
        return false;
      }
    }

    return true;
  }

  private hasRequiredRoleComposition(): boolean {
    let hasHeavy = false;
    let hasRunner = false;

    for (const player of this.state.players.values()) {
      if (!player.roleLocked) {
        continue;
      }

      hasHeavy = hasHeavy || player.role === "heavy";
      hasRunner = hasRunner || player.role === "runner";
    }

    return hasHeavy && hasRunner;
  }

  private beginRoleSelect(): void {
    this.state.phase = "role_select";
    for (const player of this.state.players.values()) {
      player.roleLocked = false;
      player.itemConfirmed = false;
      player.itemSlotA = "";
      player.itemSlotB = "";
    }
    this.refreshItemPool();
    this.refreshAnnouncement();
  }

  private beginItemSelect(): void {
    this.state.phase = "item_select";
    for (const player of this.state.players.values()) {
      player.itemConfirmed = false;
      player.itemSlotA = "";
      player.itemSlotB = "";
    }
    this.refreshItemPool();
    this.refreshAnnouncement();
  }

  private returnToRoleSelect(): void {
    this.state.phase = "role_select";
    for (const player of this.state.players.values()) {
      player.itemConfirmed = false;
      player.itemSlotA = "";
      player.itemSlotB = "";
    }
    this.refreshItemPool();
    this.state.announcement = "Adjust roles if needed, then re-enter item select.";
  }

  private returnToLobby(announcement: string): void {
    this.state.phase = "lobby";
    this.state.elapsedTime = 0;
    this.resetPlayers();
    this.resetCargo();
    this.state.announcement = announcement;
  }

  private getPlayerItems(player: PlayerState): ItemType[] {
    const items: ItemType[] = [];
    if (this.isValidItemType(player.itemSlotA)) {
      items.push(player.itemSlotA);
    }
    if (this.isValidItemType(player.itemSlotB)) {
      items.push(player.itemSlotB);
    }
    return items;
  }

  private refreshItemPool(): void {
    this.state.itemPool.blanket = this.getRemainingItemStock("blanket");
    this.state.itemPool.cart = this.getRemainingItemStock("cart");
    this.state.itemPool.stabilizer = this.getRemainingItemStock("stabilizer");
    this.state.itemPool.buffer = this.getRemainingItemStock("buffer");
  }

  private getRemainingItemStock(item: ItemType): number {
    return ITEM_POOL_TOTAL[item] - this.countSelectedItem(item);
  }

  private countSelectedItem(item: ItemType): number {
    let count = 0;
    for (const player of this.state.players.values()) {
      if (player.itemSlotA === item) {
        count += 1;
      }
      if (player.itemSlotB === item) {
        count += 1;
      }
    }
    return count;
  }

  private teamHasItem(item: ItemType): boolean {
    return this.countSelectedItem(item) > 0;
  }

  private playerHasItem(player: PlayerState, item: ItemType): boolean {
    return player.itemSlotA === item || player.itemSlotB === item;
  }

  private findLockedPlayerWithRole(role: RoleType, excludedSessionId: string): PlayerState | null {
    for (const player of this.state.players.values()) {
      if (player.sessionId === excludedSessionId) {
        continue;
      }

      if (player.roleLocked && player.role === role) {
        return player;
      }
    }

    return null;
  }

  private refreshAnnouncement(): void {
    if (this.state.players.size === 0) {
      this.state.announcement = this.state.soloDebug ? "Solo debug room is waiting for one courier." : "Waiting for couriers.";
      return;
    }

    if (this.state.phase === "lobby") {
      this.state.announcement = this.canEnterRoleSelect()
        ? this.state.soloDebug
          ? "Solo debug courier is ready. Open role select when ready."
          : "Crew is ready. Host can open role select."
        : this.state.soloDebug
          ? "Pick a debug role, then ready up."
          : "Pick Heavy Lifter or Runner, then ready up.";
      return;
    }

    if (this.state.phase === "role_select") {
      this.state.announcement = this.canEnterItemSelect()
        ? this.state.soloDebug
          ? "Debug role locked. Open the item pool when ready."
          : "Roles locked. Host can open the public item pool."
        : this.state.soloDebug
          ? "Lock your solo debug role."
          : "Lock one Heavy Lifter and one Runner.";
      return;
    }

    if (this.state.phase === "item_select") {
      this.state.announcement = this.canStartMatch()
        ? this.state.soloDebug
          ? "Solo debug loadout confirmed. Enter the run when ready."
          : "Loadouts confirmed. Host can start the run."
        : this.state.soloDebug
          ? "Choose 1-2 items and confirm to start the solo run."
          : "Each courier must confirm 1-2 items, and the team needs a blanket.";
    }
  }

  private getItemLabel(item: ItemType): string {
    switch (item) {
      case "blanket":
        return "Blanket";
      case "cart":
        return "Fold Cart";
      case "stabilizer":
        return "Stabilizer Strap";
      case "buffer":
        return "Impact Pad";
      default:
        return item;
    }
  }

  private isValidItemType(item: string): item is ItemType {
    return item === "blanket" || item === "cart" || item === "stabilizer" || item === "buffer";
  }

  private isNearCatapult(position: Vec3State): boolean {
    return this.distance3(position, CATAPULT_POSITION) <= 2.2;
  }

  private isCargoNearCatapult(): boolean {
    return this.distance3(this.state.cargo.position, CATAPULT_POSITION) <= 2.1;
  }

  private hasBlanketCarrier(): boolean {
    const carrier = this.state.players.get(this.state.cargo.carrierId);
    return carrier?.blanketActive ?? false;
  }

  private isInsideRift(position: Vec3State): boolean {
    return position.z > GAP_START_Z && position.z < GAP_END_Z && Math.abs(position.x) < 4.8;
  }

  private distance3(a: Vec3State | { x: number; y: number; z: number }, b: { x: number; y: number; z: number }): number {
    const dx = a.x - b.x;
    const dy = a.y - b.y;
    const dz = a.z - b.z;
    return Math.hypot(dx, dy, dz);
  }

  private clamp(value: number, min: number, max: number): number {
    return Math.min(max, Math.max(min, value));
  }
}
