using UnityEngine;

namespace AshenVale
{
    public sealed class RimeIceAccent : MonoBehaviour
    {
        readonly SpriteRenderer[] shards = new SpriteRenderer[8];
        SpriteRenderer flash;
        bool teleport;
        float age;
        float Duration => teleport ? .23f : .19f;

        public void Begin(bool isTeleport, int order)
        {
            teleport = isTeleport;
            flash = CreatePiece(Sprites.Disc, order);
            for (int i = 0; i < shards.Length; i++)
                shards[i] = CreatePiece(Sprites.Square, order + 1);
            Draw();
        }

        SpriteRenderer CreatePiece(Sprite sprite, int order)
        {
            var piece = new GameObject("Ice accent").AddComponent<SpriteRenderer>();
            piece.transform.SetParent(transform, false);
            piece.sprite = sprite;
            piece.sortingOrder = order;
            return piece;
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age >= Duration) Destroy(gameObject);
            else Draw();
        }

        void Draw()
        {
            float t = age / Duration;
            float spread = t * (2f - t);
            flash.transform.localPosition = Vector3.up * (teleport ? .66f : 0f);
            flash.transform.localScale = teleport
                ? new Vector3(.32f * (1f - .7f * spread), 1.35f + .2f * spread, 1f)
                : Vector3.one * (.22f + .32f * spread);
            flash.color = new Color(.67f, .94f, 1f, .66f * Mathf.Clamp01(1f - age / .065f));

            for (int i = 0; i < shards.Length; i++)
            {
                float angle = i * Mathf.PI * .25f + .21f;
                var outward = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var shard = shards[i];
                float reach = .35f + .22f * (i % 3);
                shard.transform.localPosition = teleport
                    ? new Vector3(outward.x * (.15f + .29f * spread),
                        .14f + i * .11f + .46f * spread, 0f)
                    : (Vector3)(outward * reach * spread);
                shard.transform.localRotation = Quaternion.Euler(0f, 0f,
                    teleport ? 70f + 30f * outward.x : angle * Mathf.Rad2Deg + 30f * t);
                shard.transform.localScale = new Vector3((.12f + .035f * (i % 3)) * (1f - .55f * t),
                    .027f * (1f - .7f * t), 1f);
                shard.color = new Color(.62f + .15f * (i % 2), .9f, 1f, 1f - t);
            }
        }
    }
}
