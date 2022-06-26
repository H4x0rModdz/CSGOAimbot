using CSGOAimbot.Entity;
using System.Numerics;
using System.Runtime.InteropServices;
using static CSGOAimbot.Offsets.Offset;
using CSGOAimbot.Swed;

[DllImport("user32.dll")]

extern static short GetAsyncKeyState(int vKey); // HotKey

swed swed = new swed();

Entity player = new Entity(); // memory class
List<Entity> entities = new List<Entity>(); // entities, teammates and enemies
IntPtr client, engine; // pointers

swed.GetProcess("csgo"); // csgo process here

client = swed.GetModuleBase("client.dll");
engine = swed.GetModuleBase("engine.dll");

while (true)
{
    UpdateLocalPlayer();
    UpdateEntities();

    entities = entities.OrderBy(e => e.mag).ToList();

    if (GetAsyncKeyState(0x6) < 0 && entities.Count > 0) // 0x6 == mouse5 button
    {
        foreach(var ent in entities.ToList())
        {
            if (ent.team == player.team)
                entities.Remove(ent);
        }

        if (entities.Count > 0)
            Aim(entities[0]);
    }
}

void Aim(Entity ent)
{
    #region X

    float deltaX = ent.head.X - player.feet.X;
    float deltaY = ent.head.Y - player.feet.Y;
    float X = (float)(Math.Atan2(deltaY, deltaX) * 180 / Math.PI);

    #endregion

    #region Y

    float deltaZ = ent.feet.Z - player.feet.Z;

    double dist = Math.Sqrt(Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2));

    float Y = (float)(Math.Atan2(deltaZ, dist) * 180 / Math.PI);

    var buffer = swed.ReadPointer(engine, signatures.dwClientState);

    swed.WriteBytes(buffer, signatures.dwClientState_ViewAngles, BitConverter.GetBytes(Y)); //vector2, start w/ Y
    swed.WriteBytes(buffer, signatures.dwClientState_ViewAngles + 0x4, BitConverter.GetBytes(X));

    #endregion
}

void UpdateLocalPlayer()
{
    var entityPointer = swed.ReadPointer(client, signatures.dwLocalPlayer);
    var coords = swed.ReadBytes(entityPointer, netvars.m_vecOrigin, 12);

    player.feet.X = BitConverter.ToSingle(coords, 0);
    player.feet.Y = BitConverter.ToSingle(coords, 4);
    player.feet.Z = BitConverter.ToSingle(coords, 8);

    player.team = BitConverter.ToInt32(swed.ReadBytes(entityPointer, netvars.m_iTeamNum, 4), 0);
    player.feet.Z += BitConverter.ToSingle(swed.ReadBytes(entityPointer, netvars.m_vecViewOffset+ 0x8, 4), 0); // add view vector


}

void UpdateEntities()
{
    entities.Clear();

    for (int i = 1; i < 32; i++) // loop thru entity list
    {
        var entityPointer = swed.ReadPointer(client, signatures.dwEntityList + i * 0x10);

        var team = BitConverter.ToInt32(swed.ReadBytes(entityPointer, netvars.m_iTeamNum, 4), 0);
        var dormant = BitConverter.ToInt32(swed.ReadBytes(entityPointer, signatures.m_bDormant, 4), 0);
        var hp = BitConverter.ToInt32(swed.ReadBytes(entityPointer, netvars.m_iHealth, 4), 0);

        if (hp < 2 || dormant != 0) // filter out dead enemies and non dormant ones
            continue;

        var ent = new Entity // new instance in our list
        {
            head = RecieveHead(entityPointer),
            team = team,
            health = hp
        };

        ent.mag = CalcMag(player.feet, ent.head); // calculate distance from our player to get nearest enemy

        entities.Add(ent); // add to global list
    }
}

float CalcMag(Vector3 player, Vector3 enemy)
{
    return (float)(Math.Sqrt(
        Math.Pow(enemy.X - player.X, 2) +
        Math.Pow(enemy.Y - player.Y, 2) +
        Math.Pow(enemy.Z - player.Z, 2)
        ));
}

Vector3 RecieveHead(IntPtr entPointer)
{
    var bones = swed.ReadPointer(entPointer, netvars.m_dwBoneMatrix);
    var bone = swed.ReadBytes(bones, 0x30 * 8, 0x30); // bone 8, 30 bytes in size

    return new Vector3
    {
        X = BitConverter.ToSingle(bone, 0xC), // 3 * 4 = C
        Y = BitConverter.ToSingle(bone, 0x1C),
        Z = BitConverter.ToSingle(bone, 0x2C)
    };
}