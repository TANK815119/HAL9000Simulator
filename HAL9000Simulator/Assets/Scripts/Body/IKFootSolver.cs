using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKFootSolver : MonoBehaviour
{
    [SerializeField] private Transform hip;
    [SerializeField] private Transform rotoball;
    [SerializeField] private float stepDistance;
    [SerializeField] private float stepHeight;
    [SerializeField] private float speed;
    [SerializeField] private float footOffset;
    [SerializeField] private IKFootSolver otherFoot;
    [SerializeField] private LayerMask mask;
    [SerializeField] private List<AudioClip> stepClips;
    private float ImpactCoolDown { get; set; }

    public bool stepping = false;

    private float footSpacing;
    private float lerp;
    private bool ungrounded = true;
    private bool groundImpacted = false;

    private Vector3 oldPosition;
    private Vector3 newPosition;
    private Rigidbody hipBody;
    private Vector3 lastHipPosition;
    // Start is called before the first frame update
    void Start()
    {
        footSpacing = transform.position.x - hip.position.x;
        hipBody = hip.gameObject.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        //calculate next foot placement
        float maxDist = Mathf.Abs(hip.position.y - rotoball.position.y) + 0.3f;
        Ray ray = new Ray(hip.position + (hip.right * footSpacing) + (Vector3.up * 0.2f), Vector3.down);
        if(Physics.Raycast(ray, out RaycastHit info, Mathf.Infinity, mask, QueryTriggerInteraction.Ignore))
        {
            float fastMod = 1f;
            if (fastMod < hipBody.velocity.magnitude * 0.5f) { fastMod = hipBody.velocity.magnitude * 0.5f; } //establishes speed floor
            if (!otherFoot.stepping && Vector3.Distance(newPosition, info.point) > stepDistance * fastMod)
            {
                lerp = 0;
                newPosition = info.point;
            }

            //check if player is in the air
            if(info.point.y < rotoball.position.y - 0.4f)
            {
                ungrounded = true;
            }
            else
            {
                ungrounded = false;
            }
        }

        //calculate hipDelatPosition to replace velocity
        float hipVelocity = Vector3.Distance(hipBody.transform.position, lastHipPosition) / Time.deltaTime;
        lastHipPosition = hipBody.transform.position;

        //lerp foot from old to new foot position step-by-step
        Vector3 footPosition = Vector3.zero;
        if (lerp < 1 && !otherFoot.stepping)
        {
            footPosition = Vector3.Lerp(oldPosition, newPosition, lerp);
            footPosition.y += Mathf.Sin(lerp * Mathf.PI) * stepHeight;

            stepping = true;
            float fastMod = 1f;
            if(fastMod < hipVelocity) { fastMod = hipVelocity; } //establishes speed floor
            lerp += Time.deltaTime * speed * fastMod;

            groundImpacted = false;
        }
        else
        {
            stepping = false;
            footPosition = newPosition;
            oldPosition = newPosition;

            //play step sound
            if(!groundImpacted && ImpactCoolDown <= 0f)
            {
                int footstepIndex = Random.Range(0, stepClips.Count); //may randomy give out of bounds expression! @TODO test
                AudioSource.PlayClipAtPoint(stepClips[footstepIndex], footPosition, 0.33f);

                ImpactCoolDown = 0.25f;
                otherFoot.ImpactCoolDown = 0.24f;
                groundImpacted = true;
            }
        }

        //put feet in accordane to rotonall if not touching grounf
        if(ungrounded)
        {
            newPosition = rotoball.position + (hip.right * footSpacing) + (Vector3.down * 0.4f);
            footPosition = newPosition;
        }

        if(ImpactCoolDown > 0f)
        {
            float fastMod = 1f;
            if (fastMod < hipBody.velocity.magnitude / 2f) { fastMod = hipBody.velocity.magnitude / 2f; } //establishes speed floor
            fastMod = hipBody.velocity.magnitude / 2f;
            ImpactCoolDown -= Time.deltaTime * fastMod;
        }

        transform.position = footPosition + Vector3.up * footOffset;
    }
}
