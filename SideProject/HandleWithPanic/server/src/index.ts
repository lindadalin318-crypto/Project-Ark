import express from "express";
import cors from "cors";
import { Server, WebSocketTransport } from "colyseus";
import RAPIER from "@dimforge/rapier3d-compat";
import { PanicRoom } from "./PanicRoom";

const PORT = Number(process.env.PORT ?? 2567);

async function bootstrap(): Promise<void> {
  await RAPIER.init();

  const gameServer = new Server({
    transport: new WebSocketTransport(),
    express: (app) => {
      app.use(cors());
      app.use(express.json());

      app.get("/", (_request, response) => {
        response.json({
          name: "Handle With Panic room server",
          room: "panic_room",
          status: "ok",
        });
      });

      app.get("/health", (_request, response) => {
        response.json({ ok: true });
      });
    },
  });

  gameServer.define("panic_room", PanicRoom);

  await gameServer.listen(PORT);
  console.log(`Handle With Panic server listening on http://localhost:${PORT}`);
}

bootstrap().catch((error: unknown) => {
  console.error("Failed to start the Handle With Panic server.");
  console.error(error);
  process.exit(1);
});
