using System;
using System.Collections.Generic;
using UnityEngine;

// Ashen Vale: fully self-contained 2D prototype. No third-party packages or prefabs required.
public class AshenValeGame : MonoBehaviour
{
    const float Ground = -3.15f;
    const float Gravity = -24f;
    enum Phase { Title, Select, Tutorial, Playing, Dead }
    enum Zone { Field, Town }
    int fieldIndex=0;
    bool crouching;
    float savedFieldX, savedFieldY;
    readonly List<GameObject> secondScenery = new List<GameObject>();
    readonly List<Platform> secondPlatforms = new List<Platform>();
    readonly List<Ladder> secondLadders = new List<Ladder>();
    readonly List<Foe> secondFoes = new List<Foe>();
    readonly List<Drop> secondDrops = new List<Drop>();
    readonly List<Projectile> secondShots = new List<Projectile>();
    float firstFieldX = 19f, firstFieldY = Ground;
    Zone zone = Zone.Field;
    bool inventoryOpen, shopOpen, portalOpen;
    float portalCast, returnX, returnY, portalX, portalY;
    const int InventoryCapacity=20;
    enum Rarity { Common, Rare, Epic }
    enum Slot { Weapon, Helmet, Chest, Boots }
    class Item { public string name; public Rarity rarity; public Slot slot; public int attack, defense, health, price; }
    readonly List<Item> inventory = new List<Item>();
    readonly Item[] equipped = new Item[4];
    int bonusAttack, bonusDefense, bonusHealth;
    GameObject portalObject, townObject;
    readonly List<GameObject> fieldScenery = new List<GameObject>();
    readonly List<GameObject> townScenery = new List<GameObject>();
    bool buildingField;
    const float TownPortalX = -5f;
    const float MerchantX = 3.5f;
    Phase phase = Phase.Title;
    int pendingHero=0;
    AudioSource introSource;
    bool musicMuted=false;
    float musicVolume=.30f;
    GUIStyle menuHeading, menuSubtitle, menuBody, menuSmall, menuButton, menuCard, menuSelected;
    int hero = -1, facing = 1, level = 1, xp, gold, kills, hp, maxHp, mp, maxMp, hpPotions, mpPotions;
    float x, y, vx, vy, attackCooldown, attackVisual, dashCooldown, dashTime, immunity, spawnTime, elapsed;
    bool grounded = true;
    bool climbing;
    float currentFloor = Ground;
    struct Platform { public float left, right, top; public Platform(float l,float r,float t){left=l;right=r;top=t;} }
    readonly List<Platform> platforms = new List<Platform>();
    struct Ladder { public float x, bottom, top; public Ladder(float xx,float b,float t){x=xx;bottom=b;top=t;} }
    readonly List<Ladder> ladders = new List<Ladder>();
    string animation = "idle";
    float animClock;
    float hurtVisual, deathClock;
    int comboStep;
    readonly List<SlashEffect> slashes = new List<SlashEffect>();
    class SlashEffect { public GameObject obj; public float life, duration; public int direction; public float x, y; }
    int lastAttack;
    readonly List<Foe> foes = new List<Foe>();
    readonly List<Drop> drops = new List<Drop>();
    readonly List<Projectile> shots = new List<Projectile>();
    readonly Dictionary<string, Sprite[]> sprites = new Dictionary<string, Sprite[]>();
    SpriteRenderer playerRenderer;
    Camera cam;
    Texture2D pixel;
    GUIStyle titleStyle, bodyStyle, smallStyle, centeredStyle;
    System.Random rng = new System.Random();
    class Foe { public GameObject obj; public SpriteRenderer sr; public float x, y, hp, speed, cooldown; public int tier; public float knockbackVelocity, knockbackTime; }
    class Drop { public GameObject obj; public float x, y; public int value; public bool equipment; public int potionType; public Item item; }
    class Projectile { public GameObject obj; public float x, y, direction, life; public int damage, pierce; }

    void Awake()
    {
        Application.targetFrameRate = 60;
        introSource=gameObject.AddComponent<AudioSource>();
        introSource.playOnAwake=false; introSource.loop=true;
        musicVolume=Mathf.Clamp01(PlayerPrefs.GetFloat("AshenVale.MusicVolume",.30f));
        musicMuted=PlayerPrefs.GetInt("AshenVale.MusicMuted",0)==1;
        ApplyMusicSettings();
        var intro=Resources.Load<AudioClip>("Audio/WhereTheMapsEnd_Loop_v3");
        if(intro!=null){introSource.clip=intro;introSource.Play();}
        else Debug.LogWarning("Ashen Vale: intro music missing at Assets/Resources/Audio/WhereTheMapsEnd_Loop_v3.ogg");
        pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        pixel.filterMode = FilterMode.Point;
        pixel.SetPixel(0, 0, Color.white); pixel.Apply();
        cam = Camera.main;
        if (!cam) { var c = new GameObject("Main Camera"); cam = c.AddComponent<Camera>(); c.tag = "MainCamera"; }
        cam.orthographic = true; cam.orthographicSize = 5.4f; cam.backgroundColor = new Color(.07f,.09f,.16f);
        cam.transform.position = new Vector3(0,0,-10);
        buildingField=true; MakeEnvironment(); buildingField=false;
        BuildSecondField();
        var player = new GameObject("Playable Hero"); playerRenderer = player.AddComponent<SpriteRenderer>();
        playerRenderer.sortingOrder = 10;
        playerRenderer.transform.localScale = Vector3.one * 1.65f;
        playerRenderer.enabled = false;
    }
    Sprite[] Frames(string anim)
    {
        string key = (hero == 0 ? "DragonKnight/" : "ElvenRanger/") + anim;
        if (!sprites.ContainsKey(key)) sprites[key] = Resources.LoadAll<Sprite>("Heroes/" + key);
        return sprites[key];
    }
    void StartHero(int index)
    {
        if(introSource!=null)introSource.Stop();
        ClearTown(); if(fieldIndex==1) SwitchField(0, false); if(portalObject)Destroy(portalObject); foreach(var o in fieldScenery)if(o)o.SetActive(true); hero=index; phase=Phase.Playing; zone=Zone.Field; inventoryOpen=shopOpen=portalOpen=false; portalCast=0; inventory.Clear(); Array.Clear(equipped,0,equipped.Length); bonusAttack=bonusDefense=bonusHealth=0; x=-3; y=Ground; vx=vy=0; crouching=false; climbing=false; currentFloor=Ground; grounded=true; hp=maxHp=index==0?125:95;
        level=1; xp=gold=kills=0; maxMp=index==0?80:100; mp=maxMp; hpPotions=3; mpPotions=3; spawnTime=0; attackCooldown=dashCooldown=immunity=0; elapsed=0;
        foreach(var f in foes) if(f.obj) Destroy(f.obj);
        foreach(var d in drops) if(d.obj) Destroy(d.obj);
        foreach(var p in shots) if(p.obj) Destroy(p.obj);
        foreach(var effect in slashes) if(effect.obj) Destroy(effect.obj);
        slashes.Clear(); comboStep=0; hurtVisual=deathClock=0;
        foes.Clear(); drops.Clear(); shots.Clear();
        foreach(var f in secondFoes)if(f.obj)Destroy(f.obj); secondFoes.Clear();
        foreach(var d in secondDrops)if(d.obj)Destroy(d.obj); secondDrops.Clear();
        foreach(var p in secondShots)if(p.obj)Destroy(p.obj); secondShots.Clear();
        playerRenderer.enabled=true;
        SetAnimation("idle");
    }
    void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, .04f);
        if(phase==Phase.Title){if(Input.GetKeyDown(KeyCode.M)){musicMuted=!musicMuted;SaveMusicSettings();}if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.Space))phase=Phase.Select;return;}
        if(phase==Phase.Select){
            if(Input.GetKeyDown(KeyCode.Alpha1))pendingHero=0;
            if(Input.GetKeyDown(KeyCode.Alpha2))pendingHero=1;
            if(Input.GetKeyDown(KeyCode.Return))phase=Phase.Tutorial;
            if(Input.GetKeyDown(KeyCode.Escape))phase=Phase.Title;
            return;
        }
        if(phase==Phase.Tutorial){
            if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.Space))StartHero(pendingHero);
            if(Input.GetKeyDown(KeyCode.Escape))phase=Phase.Select;
            return;
        }
        if (Input.GetKeyDown(KeyCode.R)) { StartHero(hero); return; }
        if (Input.GetKeyDown(KeyCode.I)) { inventoryOpen=!inventoryOpen; shopOpen=false; }
        if (Input.GetKeyDown(KeyCode.Escape)) { inventoryOpen=false; shopOpen=false; }
        if (zone==Zone.Town) { UpdateTown(dt); AnimateHero(dt); return; }
        if ((Input.GetKeyDown(KeyCode.W)||Input.GetKeyDown(KeyCode.UpArrow)) && grounded && ((fieldIndex==0 && x>21.0f)||(fieldIndex==1 && x< -21.0f))) { SwitchField(1-fieldIndex,true); return; }
        if (Input.GetKeyDown(KeyCode.T) && !portalOpen && portalCast<=0) portalCast=1.2f;
        if (portalCast>0) { portalCast-=dt; if(portalCast<=0) CreatePortal(); }
        if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) && portalOpen && Mathf.Abs(x-portalX)<1.5f && Mathf.Abs(y-portalY)<1.8f) { EnterTown(); return; }
        if (phase == Phase.Dead) { deathClock+=dt; AnimateHero(dt); UpdateSlashes(dt); return; }
        elapsed+=dt; hurtVisual=Mathf.Max(0,hurtVisual-dt);
        attackCooldown=Mathf.Max(0,attackCooldown-dt); attackVisual=Mathf.Max(0,attackVisual-dt);
        dashCooldown=Mathf.Max(0,dashCooldown-dt); immunity=Mathf.Max(0,immunity-dt);
        float axis=(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0);
        if(axis!=0) facing=axis>0?1:-1;
        bool jump = Input.GetKeyDown(KeyCode.Space);
        float vertical = (Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0);
        Ladder nearby = new Ladder(); bool atLadder=false;
        foreach(var ladder in ladders)
            if(Mathf.Abs(x-ladder.x)<.68f && y>=ladder.bottom-.35f && y<=ladder.top+.45f) {nearby=ladder;atLadder=true;break;}
        if(!climbing && atLadder && Mathf.Abs(vertical)>.1f) {climbing=true;grounded=false;vy=0;x=nearby.x;}
        if(climbing && (!atLadder || jump)) {climbing=false;if(jump){vy=10.8f;grounded=false;}}
        crouching = grounded && !climbing && vertical<-.1f;
        if(!climbing && grounded && jump) {vy=10.8f; grounded=false;}
        if(Input.GetKeyDown(KeyCode.L)&&dashCooldown<=0) {dashTime=.18f;dashCooldown=1.5f;immunity=.25f;}
        if(dashTime>0) {dashTime-=dt;vx=facing*14;} else vx=axis*(hero==0?5.3f:6.1f)*(crouching?.34f:1f);
        x=Mathf.Clamp(x+(climbing?0:vx*dt),-23.5f,23.5f);
        if(climbing)
        {
            vx=0;vy=0; y=Mathf.Clamp(y+vertical*4.2f*dt,nearby.bottom,nearby.top);
            if(y>=nearby.top-.04f && vertical>0){climbing=false;grounded=true;currentFloor=nearby.top;y=nearby.top;}
        }
        else
        {
            float previousY=y;
            if(!grounded){vy+=Gravity*dt;y+=vy*dt;}
            if(vy<=0)
            {
                float landing=Ground;
                foreach(var plat in platforms)
                    if(x>=plat.left+.16f && x<=plat.right-.16f && previousY>=plat.top-.03f && y<=plat.top && plat.top>landing)
                        landing=plat.top;
                if(y<=landing && previousY>=landing-.03f){y=landing;vy=0;grounded=true;currentFloor=landing;}
            }
            if(grounded)
            {
                bool supported=currentFloor==Ground;
                foreach(var plat in platforms)
                    if(Mathf.Abs(currentFloor-plat.top)<.05f && x>=plat.left+.16f && x<=plat.right-.16f) supported=true;
                if(!supported){grounded=false;vy=0;}
            }
        }
        if(Input.GetKeyDown(KeyCode.Q) && hpPotions>0 && hp<maxHp) {hpPotions--;hp=Mathf.Min(maxHp,hp+60);}
        if(Input.GetKeyDown(KeyCode.F) && mpPotions>0 && mp<maxMp) {mpPotions--;mp=Mathf.Min(maxMp,mp+45);}
        if(!crouching && !inventoryOpen && !shopOpen && Input.GetKeyDown(KeyCode.J)) Attack(false);
        if(!crouching && !inventoryOpen && !shopOpen && Input.GetKeyDown(KeyCode.K)) Attack(true);
        if(Input.GetKeyDown(KeyCode.E)) Pickup();
        string next=hurtVisual>0?"hurt":attackVisual>0?(hero==0?(lastAttack==0?(comboStep%2==0?"attack_thrust":"attack_sweep"):"dragon_strike"):(lastAttack==0?"quick_shot":"power_shot")):
            (climbing?"run":(crouching?"idle":(!grounded?(vy>0?"jump":"fall"):(Mathf.Abs(vx)>.2f?"run":"idle"))));
        SetAnimation(next); AnimateHero(dt);
        cam.transform.position=Vector3.Lerp(cam.transform.position,new Vector3(Mathf.Clamp(x,-14,14),0,-10),dt*4);
        UpdateSlashes(dt);
        UpdateFoes(dt);UpdateShots(dt);UpdateDrops(dt);
        spawnTime-=dt;
        if(spawnTime<=0&&foes.Count<9) {SpawnFoe();spawnTime=2.2f;}
    }
    void SetAnimation(string a) {if(a!=animation){animation=a;animClock=0;}}
    void AnimateHero(float dt)
    {
        animClock+=dt;
        string clip=phase==Phase.Dead?"death":animation;
        Sprite[] frames=Frames(clip);
        float fps=clip=="idle"?5:clip=="run"?11:clip=="death"?7:12;
        if(frames.Length>0)
        {
            int index=Mathf.FloorToInt(animClock*fps);
            if(clip=="death" || clip=="hurt" || clip=="dragon_strike" || clip=="attack_thrust" || clip=="attack_sweep")
                index=Mathf.Min(index,frames.Length-1);
            else index%=frames.Length;
            playerRenderer.sprite=frames[index];
        }
        playerRenderer.flipX=facing<0;
        playerRenderer.color=immunity>0&&Mathf.FloorToInt(immunity*16)%2==0?new Color(1,.55f,.55f):Color.white;
        playerRenderer.transform.localScale=new Vector3(1.65f,crouching?1.10f:1.65f,1f);
        playerRenderer.transform.position=new Vector3(x,y+(crouching?.33f:.48f),0);
    }
    void SpawnSlash(bool heavy)
    {
        // Procedural, short-lived crimson slash indicator, separate from the hero sprite.
        var obj=MakeSquare(heavy?"Dragon Strike VFX":"Spear Slash VFX",new Color(1,.1f,.17f,.8f),heavy?2.8f:1.35f,heavy?.18f:.09f,12);
        slashes.Add(new SlashEffect{obj=obj,life=heavy?.38f:.18f,duration=heavy?.38f:.18f,direction=facing,x=x+facing*(heavy?1.5f:1.1f),y=y+1.1f});
    }
    void UpdateSlashes(float dt)
    {
        for(int i=slashes.Count-1;i>=0;i--)
        {
            var fx=slashes[i];fx.life-=dt;
            if(fx.life<=0){Destroy(fx.obj);slashes.RemoveAt(i);continue;}
            var sr=fx.obj.GetComponent<SpriteRenderer>();
            sr.color=new Color(1,.12f,.18f,fx.life/fx.duration*.8f);
            fx.obj.transform.position=new Vector3(fx.x,fx.y,0);
            fx.obj.transform.rotation=Quaternion.Euler(0,0,fx.direction*(1-fx.life/fx.duration)*22);
        }
    }
    void Attack(bool skill)
    {
        if(attackCooldown>0)return;
        int manaCost=hero==0?25:30;
        if(skill && mp<manaCost)return;
        if(skill)mp-=manaCost;
        lastAttack=skill?1:0;
        if(!skill) comboStep++;
        attackCooldown=skill?(hero==0?.72f:.9f):(hero==0?.25f:.3f); attackVisual=skill?.46f:.23f;
        int damage=(hero==0?25:17)+level*4+bonusAttack+(skill?(hero==0?32:24):0);
        if(hero==1)
        {
            int count=skill?3:1;
            for(int i=0;i<count;i++)
            {
                var o=MakeSquare("Arrow",new Color(.72f,1,.43f),.52f,.11f,13);
                shots.Add(new Projectile{obj=o,x=x+facing*.7f,y=y+1.1f+(i-1)*.17f,direction=facing,damage=damage,pierce=skill?2:1,life=1.2f});
            }
        }
        else
        {
            SpawnSlash(skill);
            float range=skill?2.15f:1.4f;
            // Order targets by distance in the direction of the attack.
            // Dragon Strike pierces groups with diminishing damage; basic thrust adds knockback.
            var targets = new List<Foe>();
            foreach(var f in foes)
                if(Mathf.Abs(f.x-x)<=range && Mathf.Abs(f.y-y)<1.7f && (f.x-x)*facing>=-.3f)
                    targets.Add(f);
            targets.Sort((a,b) => ((a.x-x)*facing).CompareTo((b.x-x)*facing));
            for(int i=0;i<targets.Count;i++)
            {
                var f=targets[i];
                int dealt = skill ? Mathf.Max(1,Mathf.RoundToInt(damage * Mathf.Pow(.8f,i))) : damage;
                Hit(f,dealt);
                if(!skill && foes.Contains(f))
                {
                    f.knockbackVelocity = facing * 5.5f;
                    f.knockbackTime = .22f;
                }
                // Basic spear attacks stop after the nearest valid target.
                // Dragon Strike continues through the group with damage falloff.
                if (!skill) break;
            }
        }
    }
    void UpdateShots(float dt)
    {
        for(int i=shots.Count-1;i>=0;i--)
        {
            var p=shots[i];p.x+=p.direction*15*dt;p.life-=dt;
            p.obj.transform.position=new Vector3(p.x,p.y,0);
            foreach(var f in foes.ToArray()) if(Mathf.Abs(f.x-p.x)<.5f&&Mathf.Abs((f.y+.7f)-p.y)<1f)
            { Hit(f,p.damage);p.pierce--;p.x+=p.direction*.65f;break; }
            if(p.life<=0||p.pierce<=0) {Destroy(p.obj);shots.RemoveAt(i);}
        }
    }
    void SpawnFoe()
    {
        float sx=Mathf.Clamp(x+(rng.Next(2)==0?-1:1)*(7+rng.Next(0,5)),-22,22);
        int tier=fieldIndex==1 ? (rng.NextDouble()<.45?3:2) : (rng.NextDouble()<.17?1:0);
        var obj=MakeSquare(tier==3?"Violet Wraith":tier==2?"Moss Crawler":tier==1?"Elite Emberling":"Emberling",tier==3?new Color(.56f,.3f,.9f):tier==2?new Color(.29f,.7f,.45f):tier==1?new Color(.75f,.23f,.62f):new Color(.82f,.35f,.25f),tier==3?1.2f:tier==1?1.15f:.85f,tier==3?1.5f:tier==1?1.35f:1f,6);
        var f=new Foe{obj=obj,sr=obj.GetComponent<SpriteRenderer>(),x=sx,y=Ground,hp=tier==3?125+level*10:tier==2?65+level*7:tier==1?110+level*12:45+level*8,speed=tier==3?2.6f:tier==2?1.7f:tier==1?1.5f:2.1f,tier=tier};
        foes.Add(f);
    }
    void UpdateFoes(float dt)
    {
        foreach(var f in foes.ToArray())
        {
            float dist=x-f.x;
            if(f.knockbackTime>0f)
            {
                float step=Mathf.Min(dt,f.knockbackTime);
                f.x=Mathf.Clamp(f.x+f.knockbackVelocity*step,-23f,23f);
                f.knockbackTime=Mathf.Max(0f,f.knockbackTime-dt);
                f.knockbackVelocity=Mathf.MoveTowards(f.knockbackVelocity,0f,20f*dt);
            }
            else if(Mathf.Abs(dist)>.95f)f.x+=Mathf.Sign(dist)*f.speed*dt;
            f.obj.transform.position=new Vector3(f.x,f.y+.6f,0);
            f.cooldown-=dt;
            if(Mathf.Abs(dist)<1.1f && Mathf.Abs(f.y-y)<1.4f && f.cooldown<=0) {f.cooldown=1.15f;if(immunity<=0){hp-=Mathf.Max(1,(f.tier==3?23:f.tier==2?13:f.tier==1?19:10)-bonusDefense);immunity=.7f;hurtVisual=.23f;if(hp<=0){hp=0;phase=Phase.Dead;deathClock=0;SetAnimation("death");}}}
        }
    }
    void Hit(Foe f,int damage)
    {
        if(!foes.Contains(f))return;
        f.hp-=damage;
        if(f.hp>0)return;
        foes.Remove(f);Destroy(f.obj);kills++;
        xp+=(f.tier==3?44:f.tier==2?22:f.tier==1?32:13) + (level-1)*(f.tier==3?12:f.tier==2?7:f.tier==1?10:5);
        while(xp>=XpNeeded()){xp-=XpNeeded();level++;maxHp+=10;hp=Mathf.Min(maxHp,hp+35);maxMp+=5;mp=Mathf.Min(maxMp,mp+20);}
        // Potion drops are separate from gold/equipment and can be picked up with E.
        int potionType=rng.NextDouble()<.23 ? (rng.NextDouble()<.5?1:2) : 0;
        bool equipment=potionType==0 && rng.NextDouble()<.24;
        Item rolled=equipment?RollItem():null;
        int value=f.tier==3?27+rng.Next(17):f.tier==2?11+rng.Next(12):f.tier==1?20+rng.Next(12):5+rng.Next(10);
        var obj=MakeSquare(potionType==1?"HP Potion":potionType==2?"MP Potion":equipment?"Equipment drop":"Gold drop",potionType==1?new Color(1,.22f,.32f):potionType==2?new Color(.22f,.48f,1):equipment?new Color(.42f,.78f,1):new Color(1,.81f,.18f),.28f,.34f,7);
        drops.Add(new Drop{obj=obj,x=f.x,y=f.y+.25f,value=value,equipment=equipment,potionType=potionType,item=rolled});
        if(equipment) { var sr=obj.GetComponent<SpriteRenderer>(); sr.color=RarityColor(rolled.rarity); obj.name=rolled.name; }
    }
    void UpdateDrops(float dt)
    {
        foreach(var d in drops) if(d.obj) d.obj.transform.position=new Vector3(d.x,d.y+Mathf.Sin(elapsed*4+d.x)*.1f,0);
    }
    void Pickup()
    {
        for(int i=drops.Count-1;i>=0;i--)
        {
            var d=drops[i];if(Mathf.Abs(d.x-x)>2f || Mathf.Abs(d.y-y) > 1.8f)continue;
            if(d.potionType==1)hpPotions++; else if(d.potionType==2)mpPotions++; else if(d.equipment) { if(inventory.Count>=InventoryCapacity) continue; inventory.Add(d.item); } else gold+=d.value;
            Destroy(d.obj);drops.RemoveAt(i);
        }
    }
    GameObject MakeSquare(string name,Color color,float width,float height,int order)
    {
        var o=new GameObject(name);var s=o.AddComponent<SpriteRenderer>();
        s.sprite=SquareSprite();s.color=color;s.sortingOrder=order;o.transform.localScale=new Vector3(width,height,1);if(buildingField)fieldScenery.Add(o);return o;
    }
    Sprite square;
    Sprite SquareSprite()
    {
        if(square==null)square=Sprite.Create(pixel,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);
        return square;
    }
    void MakeEnvironment()
    {
        MakeSquare("Ground",new Color(.19f,.25f,.22f),55,1.3f,0).transform.position=new Vector3(0,Ground-.73f,0);
        MakeSquare("Ground highlight",new Color(.42f,.51f,.32f),55,.14f,1).transform.position=new Vector3(0,Ground-.07f,0);
        for(int i=0;i<45;i++)
        {
            float xx=-26+i*1.3f;
            var tree=MakeSquare("Distant forest",i%3==0?new Color(.12f,.25f,.29f):new Color(.12f,.2f,.25f),.65f,2+(i%5)*.6f,-3);
            tree.transform.position=new Vector3(xx,-1.7f,0);
        }
        // Solid-looking, one-way platforms. The character lands when crossing a top from above.
        AddPlatform(-18f,-12f,-.95f);
        AddPlatform(-9f,-3.5f,.55f);
        AddPlatform(1.5f,7f,-.85f);
        AddPlatform(10f,16f,.55f);
        AddPlatform(18f,22f,-.95f);
        AddLadder(-15f,Ground,-.95f);
        AddLadder(-6f,Ground,.55f);
        AddLadder(4f,Ground,-.85f);
        AddLadder(13f,Ground,.55f);
        AddLadder(20f,Ground,-.95f);
        for(int i=0;i<18;i++)
        {
            var stone=MakeSquare("Stone",new Color(.31f,.36f,.39f),.45f+(i%3)*.2f,.2f,2);
            stone.transform.position=new Vector3(-23+i*2.7f,Ground+.08f,0);
        }
    }
    void BuildSecondField()
    {
        // Build and store a separate map, then restore the first map's geometry.
        var originalObjects=new List<GameObject>(fieldScenery);
        var originalPlatforms=new List<Platform>(platforms);
        var originalLadders=new List<Ladder>(ladders);
        fieldScenery.Clear(); platforms.Clear(); ladders.Clear();
        buildingField=true;
        MakeSquare("Mosswood ground",new Color(.13f,.25f,.20f),55,1.3f,0).transform.position=new Vector3(0,Ground-.73f,0);
        MakeSquare("Mosswood grass",new Color(.28f,.58f,.35f),55,.14f,1).transform.position=new Vector3(0,Ground-.07f,0);
        for(int i=0;i<38;i++) {
            var tree=MakeSquare("Mosswood silhouette",i%3==0?new Color(.11f,.28f,.27f):new Color(.08f,.21f,.19f),.6f+(i%3)*.2f,2.5f+(i%5)*.5f,-3);
            tree.transform.position=new Vector3(-25+i*1.4f,-1.65f,0);
        }
        AddPlatform(-18,-11,-1.2f); AddPlatform(-8,-2,.35f); AddPlatform(2,9,-.6f); AddPlatform(12,19,.65f);
        AddLadder(-14.5f,Ground,-1.2f); AddLadder(-5,Ground,.35f); AddLadder(5.5f,Ground,-.6f); AddLadder(15.5f,Ground,.65f);
        var portal=MakeSquare("Mosswood return portal - UP",new Color(.4f,.75f,1,.8f),.9f,1.9f,8);
        portal.transform.position=new Vector3(-22.5f,Ground+.9f,0);
        buildingField=false;
        secondScenery.AddRange(fieldScenery); secondPlatforms.AddRange(platforms); secondLadders.AddRange(ladders);
        foreach(var o in secondScenery)if(o)o.SetActive(false);
        fieldScenery.Clear();fieldScenery.AddRange(originalObjects);
        platforms.Clear();platforms.AddRange(originalPlatforms);
        ladders.Clear();ladders.AddRange(originalLadders);
        var gate=MakeSquare("Mosswood entrance portal - UP",new Color(.35f,.85f,.65f,.85f),.9f,1.9f,8);
        gate.transform.position=new Vector3(22.5f,Ground+.9f,0);
        fieldScenery.Add(gate);
    }
    void SwitchField(int target, bool moveHero)
    {
        // Swap world objects and enemy/drop state, preserving both hunting maps.
        if(target==fieldIndex)return;
        if(fieldIndex==0){firstFieldX=x;firstFieldY=y;}
        foreach(var o in fieldScenery)if(o)o.SetActive(false);
        foreach(var f in foes)if(f.obj)f.obj.SetActive(false);
        foreach(var d in drops)if(d.obj)d.obj.SetActive(false);
        foreach(var p in shots)if(p.obj)p.obj.SetActive(false);
        foreach(var fx in slashes)if(fx.obj)Destroy(fx.obj);
        slashes.Clear();
        SwapList(fieldScenery,secondScenery); SwapList(platforms,secondPlatforms); SwapList(ladders,secondLadders);
        SwapList(foes,secondFoes); SwapList(drops,secondDrops); SwapList(shots,secondShots);
        fieldIndex=target;
        foreach(var o in fieldScenery)if(o)o.SetActive(true);
        foreach(var f in foes)if(f.obj)f.obj.SetActive(true);
        foreach(var d in drops)if(d.obj)d.obj.SetActive(true);
        foreach(var p in shots)if(p.obj)p.obj.SetActive(true);
        if(moveHero){x=target==1?-19.5f:19.5f;y=Ground;vx=vy=0;grounded=true;climbing=crouching=false;currentFloor=Ground;}
        if(portalObject)portalObject.SetActive(false);
        portalOpen=false;portalCast=0;
        cam.transform.position=new Vector3(Mathf.Clamp(x,-14,14),0,-10);
    }
    static void SwapList<T>(List<T> a,List<T> b)
    {
        var temp=new List<T>(a);a.Clear();a.AddRange(b);b.Clear();b.AddRange(temp);
    }
    void AddPlatform(float left,float right,float top)
    {
        platforms.Add(new Platform(left,right,top));
        float mid=(left+right)*.5f;
        MakeSquare("Platform earth",new Color(.25f,.29f,.27f),right-left,.32f,2).transform.position=new Vector3(mid,top-.18f,0);
        MakeSquare("Platform grass",new Color(.47f,.64f,.32f),right-left,.16f,3).transform.position=new Vector3(mid,top-.04f,0);
        for(float xx=left+.4f;xx<right;xx+=1.3f)
            MakeSquare("Platform support",new Color(.23f,.25f,.27f),.18f,.65f,1).transform.position=new Vector3(xx,top-.53f,0);
    }
    void AddLadder(float xx,float bottom,float top)
    {
        ladders.Add(new Ladder(xx,bottom,top));
        float h=top-bottom;
        for(int side=-1;side<=1;side+=2)
            MakeSquare("Ladder rail",new Color(.55f,.35f,.19f),.11f,h,4).transform.position=new Vector3(xx+side*.27f,(bottom+top)*.5f,0);
        for(float yy=bottom+.24f;yy<top;yy+=.38f)
            MakeSquare("Ladder rung",new Color(.72f,.51f,.28f),.65f,.09f,5).transform.position=new Vector3(xx,yy,0);
    }
    int XpNeeded() { return 35 + level*25; }
    Color RarityColor(Rarity r) { return r==Rarity.Epic?new Color(.72f,.35f,1):r==Rarity.Rare?new Color(.3f,.65f,1):new Color(.8f,.8f,.8f); }
    Item RollItem()
    {
        Slot slot=(Slot)rng.Next(4);
        double roll=rng.NextDouble(); Rarity rarity=roll<.05?Rarity.Epic:roll<.25?Rarity.Rare:Rarity.Common;
        int mult=rarity==Rarity.Epic?3:rarity==Rarity.Rare?2:1;
        int power=1+level/3;
        string[] names={"Spear", "Helmet", "Armor", "Boots"};
        return new Item { name=rarity+" "+names[(int)slot],slot=slot,rarity=rarity,
            attack=slot==Slot.Weapon?mult*(2+power):0,
            defense=slot==Slot.Helmet||slot==Slot.Chest?mult*(1+power/2):0,
            health=slot==Slot.Boots||slot==Slot.Chest?mult*(4+power*2):0,
            price=(25+level*10)*mult*mult };
    }
    void RecalculateEquipment()
    {
        int previous=bonusHealth; bonusAttack=bonusDefense=bonusHealth=0;
        foreach(Item item in equipped) if(item!=null){bonusAttack+=item.attack;bonusDefense+=item.defense;bonusHealth+=item.health;}
        maxHp+=bonusHealth-previous;hp=Mathf.Clamp(hp+Mathf.Max(0,bonusHealth-previous),1,maxHp);
    }
    void EquipAt(int index)
    {
        if(index<0||index>=inventory.Count)return;
        Item item=inventory[index];int slot=(int)item.slot;
        inventory.RemoveAt(index);
        if(equipped[slot]!=null) inventory.Add(equipped[slot]);
        equipped[slot]=item;RecalculateEquipment();
    }
    void SellAt(int index)
    {
        if(index<0||index>=inventory.Count)return;
        gold+=Mathf.Max(1,inventory[index].price/2);inventory.RemoveAt(index);
    }
    void CreatePortal()
    {
        portalOpen=true;portalX=x;portalY=y;
        if(portalObject)Destroy(portalObject);
        portalObject=MakeSquare("Town Portal (Up)",new Color(.3f,.35f,1,.75f),.85f,1.7f,8);
        portalObject.transform.position=new Vector3(portalX,portalY+.8f,0);
    }
    void TownBlock(string name, Color color, float w, float h, float px, float py, int order)
    {
        var o=MakeSquare(name,color,w,h,order);
        o.transform.position=new Vector3(px,py,0);
        townScenery.Add(o);
    }
    void BuildTown()
    {
        // Separate, fully visible town environment; hunting map is hidden, not destroyed.
        TownBlock("Emberhaven cobblestone",new Color(.30f,.27f,.25f),34,1.3f,0,Ground-.73f,0);
        TownBlock("Emberhaven street",new Color(.65f,.53f,.37f),34,.15f,0,Ground-.06f,1);
        TownBlock("Town sky",new Color(.12f,.20f,.27f),34,10,0,1,-10);
        for(int i=0;i<9;i++)
        {
            float xx=-14+i*3.5f;
            TownBlock("Town backdrop house",new Color(.36f,.28f,.28f),2.8f,2.3f,xx,Ground+1.12f,-3);
            TownBlock("Town roof",new Color(.54f,.25f,.22f),3.15f,.5f,xx,Ground+2.45f,-2);
            TownBlock("Warm window",new Color(1f,.72f,.34f),.5f,.65f,xx,Ground+1.3f,-1);
        }
        TownBlock("Return portal glow",new Color(.23f,.45f,1,.8f),1.1f,2.2f,TownPortalX,Ground+1.1f,7);
        TownBlock("Return portal core",new Color(.58f,.78f,1,.85f),.55f,1.7f,TownPortalX,Ground+1.1f,8);
        TownBlock("Merchant stall",new Color(.53f,.31f,.18f),3.3f,1.3f,MerchantX,Ground+.75f,5);
        TownBlock("Merchant awning",new Color(.82f,.45f,.19f),3.8f,.45f,MerchantX,Ground+1.7f,6);
        TownBlock("Merchant NPC",new Color(.91f,.76f,.44f),.65f,1.25f,MerchantX,Ground+.75f,9);
        TownBlock("Merchant sign",new Color(.93f,.77f,.45f),1.5f,.32f,MerchantX,Ground+2.2f,7);
    }
    void ClearTown()
    {
        foreach(var o in townScenery)if(o)Destroy(o);
        townScenery.Clear();
    }
    void EnterTown()
    {
        returnX=x;returnY=y;zone=Zone.Town; x=TownPortalX+1.6f;y=Ground;vx=vy=0;
        grounded=true;climbing=false;currentFloor=Ground;shopOpen=inventoryOpen=false;
        foreach(var o in fieldScenery)if(o)o.SetActive(false);
        if(portalObject)portalObject.SetActive(false);
        foreach(var f in foes)if(f.obj)f.obj.SetActive(false);
        foreach(var d in drops)if(d.obj)d.obj.SetActive(false);
        foreach(var shot in shots)if(shot.obj)shot.obj.SetActive(false);
        foreach(var fx in slashes)if(fx.obj)fx.obj.SetActive(false);
        BuildTown();cam.transform.position=new Vector3(0,0,-10);
        SetAnimation("idle");
    }
    void ReturnToField()
    {
        zone=Zone.Field;shopOpen=inventoryOpen=false;x=returnX;y=returnY;vx=vy=0;
        grounded=true;climbing=false;currentFloor=y;
        ClearTown();
        foreach(var o in fieldScenery)if(o)o.SetActive(true);
        if(portalObject)portalObject.SetActive(true);
        foreach(var f in foes)if(f.obj)f.obj.SetActive(true);
        foreach(var d in drops)if(d.obj)d.obj.SetActive(true);
        foreach(var shot in shots)if(shot.obj)shot.obj.SetActive(true);
        foreach(var fx in slashes)if(fx.obj)fx.obj.SetActive(true);
        cam.transform.position=new Vector3(Mathf.Clamp(x,-14,14),0,-10);
        spawnTime=Mathf.Max(1,spawnTime);
    }
    void UpdateTown(float dt)
    {
        // Real movement in town instead of a frozen player at a static merchant screen.
        float axis=(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-
                   (Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0);
        if(axis!=0)facing=axis>0?1:-1;
        vx=axis*(hero==0?5.3f:6.1f);
        x=Mathf.Clamp(x+vx*dt,-14,14);
        if(Input.GetKeyDown(KeyCode.Space)&&grounded){vy=10.8f;grounded=false;}
        if(!grounded){vy+=Gravity*dt;y+=vy*dt;if(y<=Ground){y=Ground;vy=0;grounded=true;}}
        if((Input.GetKeyDown(KeyCode.W)||Input.GetKeyDown(KeyCode.UpArrow)) && Mathf.Abs(x-TownPortalX)<1.35f){ReturnToField();return;}
        if(Input.GetKeyDown(KeyCode.E) && Mathf.Abs(x-MerchantX)<2f){shopOpen=!shopOpen;inventoryOpen=false;}
        if(Input.GetKeyDown(KeyCode.B)&&Mathf.Abs(x-MerchantX)<2f){shopOpen=!shopOpen;inventoryOpen=false;}
        if(Input.GetKeyDown(KeyCode.Q)&&hpPotions>0&&hp<maxHp){hpPotions--;hp=Mathf.Min(maxHp,hp+60);}
        if(Input.GetKeyDown(KeyCode.F)&&mpPotions>0&&mp<maxMp){mpPotions--;mp=Mathf.Min(maxMp,mp+45);}
        SetAnimation(!grounded?(vy>0?"jump":"fall"):(Mathf.Abs(vx)>.1f?"run":"idle"));
        AnimateHero(dt);
        cam.transform.position=Vector3.Lerp(cam.transform.position,new Vector3(Mathf.Clamp(x,-7,7),0,-10),dt*6);
    }
    Rect PanelRect() { return new Rect(Mathf.Max(10,(Screen.width-630)/2f),Mathf.Max(120,(Screen.height-480)/2f),630,480); }
    void DrawInventory()
    {
        Rect r=PanelRect();GUI.Box(r,"INVENTORY — 20 SLOTS   [I] Close");
        GUI.Label(new Rect(r.x+18,r.y+25,580,22),"Click item to equip. Gear bonuses: ATK +"+bonusAttack+"  DEF +"+bonusDefense+"  HP +"+bonusHealth,smallStyle);
        for(int i=0;i<4;i++)
        {
            Item item=equipped[i];
            GUI.Label(new Rect(r.x+18+(i%2)*300,r.y+52+(i/2)*27,290,25),((Slot)i)+": "+(item==null?"Empty":item.name),smallStyle);
        }
        for(int i=0;i<InventoryCapacity;i++)
        {
            int col=i%5,row=i/5; Rect cell=new Rect(r.x+18+col*120,r.y+119+row*75,115,68);
            Item item=i<inventory.Count?inventory[i]:null;
            if(item!=null)
            {
                Color old=GUI.color;GUI.color=RarityColor(item.rarity);
                if(GUI.Button(cell,item.name+"\n+"+item.attack+" ATK / +"+item.defense+" DEF\n+"+item.health+" HP"))EquipAt(i);
                GUI.color=old;
            }
            else GUI.Box(cell,"—");
        }
        GUI.Label(new Rect(r.x+18,r.y+430,570,25),"Equipment drops use 75% Common / 20% Rare / 5% Epic.",smallStyle);
    }
    void DrawShop()
    {
        Rect r=PanelRect();GUI.Box(r,"EMBERHAVEN GENERAL STORE — [B] Close");
        GUI.Label(new Rect(r.x+20,r.y+32,550,28),"Gold: "+gold+"   |   Sell unwanted gear for half its value",bodyStyle);
        if(GUI.Button(new Rect(r.x+20,r.y+70,270,42),"Buy HP Potion — 25 gold")&&gold>=25){gold-=25;hpPotions++;}
        if(GUI.Button(new Rect(r.x+320,r.y+70,270,42),"Buy MP Potion — 35 gold")&&gold>=35){gold-=35;mpPotions++;}
        GUI.Label(new Rect(r.x+20,r.y+123,560,25),"Merchant equipment (reliable Common gear)",smallStyle);
        for(int i=0;i<4;i++)
        {
            int cost=150+level*25+(i==0?100:0);Slot slot=(Slot)i;
            if(GUI.Button(new Rect(r.x+20+(i%2)*300,r.y+154+(i/2)*49,270,40),"Buy Common "+slot+" — "+cost+"g")&&gold>=cost&&inventory.Count<InventoryCapacity)
            {
                Item item=new Item{name="Common "+slot,slot=slot,rarity=Rarity.Common,price=cost,attack=i==0?3+level/3:0,defense=i==1||i==2?2+level/4:0,health=i==2||i==3?6+level:0};
                gold-=cost;inventory.Add(item);
            }
        }
        GUI.Label(new Rect(r.x+20,r.y+269,550,25),"Sell inventory items",smallStyle);
        for(int i=0;i<inventory.Count&&i<10;i++)
        {
            Item item=inventory[i];int col=i%2,row=i/2;
            if(GUI.Button(new Rect(r.x+20+col*300,r.y+297+row*31,270,29),"Sell "+item.name+" +"+(item.price/2)+"g")){SellAt(i);break;}
        }
    }
    void ApplyMusicSettings()
    {
        if(introSource!=null){introSource.volume=musicVolume;introSource.mute=musicMuted;}
    }
    void SaveMusicSettings()
    {
        ApplyMusicSettings();
        PlayerPrefs.SetFloat("AshenVale.MusicVolume",musicVolume);
        PlayerPrefs.SetInt("AshenVale.MusicMuted",musicMuted?1:0);
        PlayerPrefs.Save();
    }
    void InitMenuStyles()
    {
        if(menuHeading!=null)return;
        menuHeading=new GUIStyle(GUI.skin.label){fontSize=39,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
        menuHeading.normal.textColor=new Color(1f,.84f,.58f);
        menuSubtitle=new GUIStyle(GUI.skin.label){fontSize=19,alignment=TextAnchor.MiddleCenter};
        menuSubtitle.normal.textColor=new Color(.78f,.85f,.91f);
        menuBody=new GUIStyle(GUI.skin.label){fontSize=17,alignment=TextAnchor.MiddleCenter,wordWrap=true};
        menuBody.normal.textColor=new Color(.93f,.94f,.96f);
        menuSmall=new GUIStyle(menuBody){fontSize=14};
        menuButton=new GUIStyle(GUI.skin.button){fontSize=18,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
        menuCard=new GUIStyle(GUI.skin.box){fontSize=17,alignment=TextAnchor.UpperCenter,wordWrap=true};
        menuSelected=new GUIStyle(menuCard);
        menuSelected.normal.textColor=new Color(1f,.84f,.58f);
    }
    void DrawHeroCard(Rect r,int index)
    {
        bool selected=pendingHero==index;
        Color previous=GUI.color;
        GUI.color=selected?new Color(1f,.85f,.57f):new Color(.72f,.78f,.87f);
        GUI.Box(r,GUIContent.none,selected?menuSelected:menuCard);
        GUI.color=previous;
        string name=index==0?"DRAGON KNIGHT":"ELVEN RANGER";
        GUI.Label(new Rect(r.x+10,r.y+10,r.width-20,29),name,menuSubtitle);
        // Preview uses the same Resources sprites as the playable character, when available.
        string resource=index==0?"Heroes/DragonKnight/idle":"Heroes/ElvenRanger/idle";
        Sprite[] preview=Resources.LoadAll<Sprite>(resource);
        if(preview.Length>0 && preview[0]!=null)
        {
            Sprite sprite=preview[0];
            Rect tex=sprite.textureRect;
            float aspect=tex.width/Mathf.Max(1f,tex.height);
            float height=91f,width=Mathf.Min(110f,height*aspect);
            Rect target=new Rect(r.center.x-width/2,r.y+43,width,height);
            GUI.DrawTextureWithTexCoords(target,sprite.texture,new Rect(tex.x/sprite.texture.width,tex.y/sprite.texture.height,tex.width/sprite.texture.width,tex.height/sprite.texture.height),true);
        }
        else GUI.Label(new Rect(r.x+15,r.y+64,r.width-30,55),index==0?"SPEAR / MELEE":"BOW / RANGED",menuBody);
        GUI.Label(new Rect(r.x+12,r.y+140,r.width-24,44),index==0?"Heavy strikes  •  Durable":"Agile attacks  •  Long range",menuSmall);
        GUI.Label(new Rect(r.x+12,r.y+181,r.width-24,25),index==0?"125 HP     80 MP":"95 HP     100 MP",menuSmall);
        if(GUI.Button(new Rect(r.x+20,r.y+r.height-45,r.width-40,34),selected?"SELECTED":"SELECT HERO",menuButton))pendingHero=index;
    }
    void DrawFrontMenu()
    {
        InitMenuStyles();
        // Reference-space layout scales as one unit, keeping labels/buttons aligned.
        float scale=Mathf.Min(Screen.width/900f,Screen.height/610f);
        scale=Mathf.Max(.1f,scale);
        Matrix4x4 oldMatrix=GUI.matrix;
        GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
        float sw=Screen.width/scale,sh=Screen.height/scale;
        Rect panel=new Rect((sw-840)/2,(sh-550)/2,840,550);
        GUI.Box(panel,GUIContent.none);
        float left=panel.x,top=panel.y;
        if(phase==Phase.Title)
        {
            GUI.Label(new Rect(left+35,top+40,770,65),"ASHEN VALE",menuHeading);
            GUI.Label(new Rect(left+35,top+110,770,35),"Where the Maps End",menuSubtitle);
            GUI.Label(new Rect(left+95,top+174,650,72),"Beyond the last lantern, the wilds remember.\nA new journey begins in Emberhaven.",menuBody);
            if(GUI.Button(new Rect(left+295,top+278,250,49),"START ADVENTURE",menuButton))phase=Phase.Select;
            GUI.Label(new Rect(left+265,top+354,310,27),"MUSIC VOLUME",menuSubtitle);
            float updated=GUI.HorizontalSlider(new Rect(left+287,top+400,215,20),musicVolume,0f,1f);
            if(Mathf.Abs(updated-musicVolume)>.001f){musicVolume=updated;SaveMusicSettings();}
            GUI.Label(new Rect(left+510,top+388,65,30),Mathf.RoundToInt(musicVolume*100)+"%",menuSmall);
            if(GUI.Button(new Rect(left+322,top+432,196,33),musicMuted?"UNMUTE  [M]":"MUTE  [M]")) {musicMuted=!musicMuted;SaveMusicSettings();}
            GUI.Label(new Rect(left+40,top+500,760,24),"A whimsical journey into the unknown",menuSmall);
        }
        else if(phase==Phase.Select)
        {
            GUI.Label(new Rect(left+40,top+20,760,54),"CHOOSE YOUR HERO",menuHeading);
            GUI.Label(new Rect(left+40,top+76,760,28),"Every legend begins with a choice.",menuSubtitle);
            DrawHeroCard(new Rect(left+80,top+115,325,280),0);
            DrawHeroCard(new Rect(left+435,top+115,325,280),1);
            if(GUI.Button(new Rect(left+290,top+424,260,47),"CONTINUE  [ENTER]",menuButton))phase=Phase.Tutorial;
            if(GUI.Button(new Rect(left+342,top+486,156,32),"BACK  [ESC]"))phase=Phase.Title;
        }
        else
        {
            GUI.Label(new Rect(left+30,top+19,780,54),"YOUR FIRST ADVENTURE",menuHeading);
            GUI.Label(new Rect(left+55,top+78,730,30),"A few essentials before you enter Emberfields",menuSubtitle);
            string[] keys={"A / D  or  ← / →","SPACE","W / S","S / DOWN","J / K","Q / F","E / I","T / W","L"};
            string[] actions={"Move","Jump","Climb ladders","Crouch","Attack / Special attack","HP / MP potions","Collect loot / Inventory","Create / Enter portal","Dash"};
            for(int i=0;i<keys.Length;i++)
            {
                float yy=top+125+i*33;
                if(i%2==0)GUI.Box(new Rect(left+105,yy,630,31),GUIContent.none);
                GUI.Label(new Rect(left+124,yy+2,275,27),keys[i],menuSmall);
                GUI.Label(new Rect(left+405,yy+2,305,27),actions[i],menuSmall);
            }
            GUI.Label(new Rect(left+60,top+432,720,29),"Hunt monsters, collect equipment, and visit Emberhaven to restock.",menuSmall);
            if(GUI.Button(new Rect(left+282,top+468,276,44),"BEGIN ADVENTURE",menuButton))StartHero(pendingHero);
            if(GUI.Button(new Rect(left+362,top+517,116,25),"BACK  [ESC]"))phase=Phase.Select;
        }
        GUI.matrix=oldMatrix;
    }
    // Compact top-left RPG HUD, using built-in IMGUI textures (no external assets).
    void HudFill(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    void HudBar(float xPos, float yPos, float width, string label, int current, int maximum, Color fill)
    {
        float ratio = maximum > 0 ? Mathf.Clamp01((float)current / maximum) : 0f;
        HudFill(new Rect(xPos, yPos, width, 24), new Color(.055f,.075f,.12f,.95f));
        HudFill(new Rect(xPos+2, yPos+2, (width-4)*ratio, 20), fill);
        GUI.Label(new Rect(xPos+9,yPos+2,width-18,20),label+"   "+current+" / "+maximum,hudBarText);
    }

    GUIStyle hudTitleText, hudBarText, hudDetailText;
    void DrawGameplayHud()
    {
        if(hudTitleText==null)
        {
            hudTitleText=new GUIStyle(GUI.skin.label){fontSize=16,fontStyle=FontStyle.Bold,normal={textColor=new Color(1f,.85f,.57f)}};
            hudBarText=new GUIStyle(GUI.skin.label){fontSize=12,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleLeft,normal={textColor=Color.white}};
            hudDetailText=new GUIStyle(GUI.skin.label){fontSize=12,normal={textColor=new Color(.88f,.9f,.94f)}};
        }
        Matrix4x4 oldMatrix=GUI.matrix;
        float scale=Mathf.Min(1f, Mathf.Max(.65f, Screen.width/420f));
        GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
        HudFill(new Rect(10,10,364,195),new Color(.055f,.075f,.12f,.90f));
        HudFill(new Rect(10,10,364,3),new Color(.65f,.48f,.27f));
        GUI.Label(new Rect(23,17,300,25),hero==0?"DRAGON KNIGHT":"ELVEN RANGER",hudTitleText);
        GUI.Label(new Rect(310,19,55,22),"LV "+level,hudDetailText);
        HudBar(23,49,338,"HP",hp,maxHp,new Color(.82f,.20f,.25f));
        HudBar(23,80,338,"MP",mp,maxMp,new Color(.19f,.46f,.88f));
        HudBar(23,111,338,"XP",xp,XpNeeded(),new Color(.88f,.68f,.24f));
        HudFill(new Rect(23,144,338,1),new Color(.45f,.45f,.48f,.55f));
        GUI.Label(new Rect(23,152,220,20),"GOLD  "+gold+"     KILLS  "+kills,hudDetailText);
        GUI.Label(new Rect(23,175,338,19),"HP POTIONS [Q]  "+hpPotions+"       MP POTIONS [F]  "+mpPotions,hudDetailText);
        GUI.matrix=oldMatrix;
    }

    void OnGUI()
    {
        if(titleStyle==null)
        {
            titleStyle=new GUIStyle(GUI.skin.label){fontSize=25,fontStyle=FontStyle.Bold,normal={textColor=new Color(1,.82f,.53f)}};
            bodyStyle=new GUIStyle(GUI.skin.label){fontSize=17,normal={textColor=Color.white}};
            smallStyle=new GUIStyle(GUI.skin.label){fontSize=14,normal={textColor=new Color(.82f,.85f,.9f)}};
            centeredStyle=new GUIStyle(bodyStyle){alignment=TextAnchor.MiddleCenter,fontSize=21};
        }
        if(phase==Phase.Title || phase==Phase.Select || phase==Phase.Tutorial)
        {
            DrawFrontMenu();
            return;
        }
        DrawGameplayHud();
            GUI.Box(new Rect(10,Screen.height-48,Mathf.Min(Screen.width-20,920),38),GUIContent.none);
            GUI.Label(new Rect(22,Screen.height-42,1250,30),zone==Zone.Town?"A/D Move | W/Up at blue portal: Return | E at merchant: Shop | I Inventory":"A/D Move | S/Down Crouch/Crawl | SPACE Jump | W/S Ladder | J/K Attack | Q/F Potions | E Loot | W/Up Portal | T Town | I Bag",smallStyle);
            if(inventoryOpen) DrawInventory();
            if(shopOpen && zone==Zone.Town) DrawShop();
            if(zone==Zone.Town) GUI.Label(new Rect(18,218,800,35),"EMBERHAVEN — WALK TO MERCHANT (RIGHT) OR BLUE PORTAL (LEFT), PRESS UP AT PORTAL / E AT MERCHANT",bodyStyle);
            if(zone==Zone.Field)GUI.Label(new Rect(18,218,850,35),fieldIndex==0?"EMBERFIELDS — GREEN GATE AT FAR RIGHT: UP TO MOSSWOOD":"MOSSWOOD — BLUE GATE AT FAR LEFT: UP TO EMBERFIELDS",bodyStyle);
            if(portalCast>0) GUI.Label(new Rect(18,245,500,35),"Casting Town Portal...",bodyStyle);
            if(phase==Phase.Dead)GUI.Label(new Rect(0,Screen.height/2f-25,Screen.width,65),"YOU FELL IN BATTLE — PRESS R TO RESTART",centeredStyle);
    }
}
