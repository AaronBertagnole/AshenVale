using UnityEngine;
namespace AshenVale {
 public sealed class RimeTransientFx:MonoBehaviour {
  public float duration=.35f;float age;SpriteRenderer sr;
  void Awake(){sr=GetComponent<SpriteRenderer>();}
  void Update(){age+=Time.deltaTime;if(age>=duration){Destroy(gameObject);return;}var c=sr.color;c.a=1-age/duration;sr.color=c;}
 }
}
