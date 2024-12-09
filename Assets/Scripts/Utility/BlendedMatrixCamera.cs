using System.Collections;
using UnityEngine;

namespace KHI.Camera
{
	[RequireComponent(typeof(UnityEngine.Camera))]
	public class BlendedMatrixCamera : MonoBehaviour {

		[Tooltip("Objects located here stay the same size on perspective distortion")]
	    public Transform pivot = null;

		[SerializeField]
	    [Tooltip("Zero for parallel projection, negative for reverse perspective")]
	    float _perspective = 0.0f;

	    public float Perspective
	    {
		    get => _perspective;
		    set
		    {
			    _perspective = value;
			    UpdateProjection();
		    }
	    }

	    public float ZoomDistanceLevel
	    {
		    get => m_camera.orthographicSize;
		    set
		    {
			    m_camera.transform.localPosition = Vector3.back * (value * 2);
			    m_camera.orthographicSize = value;
			    //RecalculatePartialProjectionMatrix();
			    //UpdateProjection();
			    
			    // Start or sustain reprojection loop
			    if (delayedReprojectionLoop == null)
				    delayedReprojectionLoop = StartCoroutine(DelayedReprojection());
			    else
				    reprojectionRequested = true;
		    }
	    }
	    

		private UnityEngine.Camera m_camera;

		private float m_m00;
		private float m_m11;

	    void Reset()
	    {
	        m_camera = GetComponent<UnityEngine.Camera>();
		    m_camera.orthographic = true;

	        RecalculatePartialProjectionMatrix();
			UpdateProjection();

		}

	    bool reprojectionRequested = false;
	    Coroutine delayedReprojectionLoop = null;
	    IEnumerator DelayedReprojection()
	    {
		    while (reprojectionRequested)
		    {
			    reprojectionRequested = false;
			    RecalculatePartialProjectionMatrix();
			    UpdateProjection();
			    yield return null;
		    }
	    }
	    

	    void Start()
	    {
	        Reset();
	    }

	    /// <summary>
	    /// Preview camera settings in Editor mode without setting projection each update
	    /// </summary>
		void OnValidate()
		{
			m_camera = GetComponent<UnityEngine.Camera>();
			RecalculatePartialProjectionMatrix();
			UpdateProjection();
		}

		public void UpdateProjection()
		{
			float distance = GetCameraDistance();
			float near = m_camera.nearClipPlane;
			float far = m_camera.farClipPlane;

			Matrix4x4 matrix = GetProjectionMatrix(-Perspective * 0.01f, -distance);

			// Apply the corrected matrix
			m_camera.projectionMatrix = matrix;
			m_camera.nonJitteredProjectionMatrix = matrix;

			// Ensure physical properties are respected
			m_camera.usePhysicalProperties = true;

			// Debug to ensure shadow cascades are calculated properly
			Debug.Log("Projection Matrix Updated for Shadows");
		}

		private float GetCameraDistance() 
		{
			if (pivot != null) 
			{
				return (pivot.position - this.transform.position).magnitude;
			}
			else
			{
				return this.transform.position.magnitude;
			}
		}

		private void RecalculatePartialProjectionMatrix()
		{
			m_m00 = 1f / m_camera.orthographicSize / m_camera.aspect;
			m_m11 = 1f / m_camera.orthographicSize;
		}

		private Matrix4x4 GetProjectionMatrix(float p, float d)
		{
			float near = m_camera.nearClipPlane;
			float far = m_camera.farClipPlane;

			var m = new Matrix4x4();
			m.m00 = m_m00;
			m.m11 = m_m11;
			m.m22 = -(far + near) / (far - near); // Depth range correction
			m.m23 = -2f * far * near / (far - near); // Depth range correction
			m.m32 = p;
			m.m33 = 1.0f - d * p;

			return m;
		}



		void OnDrawGizmosSelected()
		{
			Matrix4x4 oldGizmosMatrix = Gizmos.matrix;

			Gizmos.matrix = this.transform.localToWorldMatrix;
			Gizmos.color = new Color(0.6f, 0.6f, 0.9f, 0.5f);

			float d = GetCameraDistance();
			float p = Perspective * 0.01f;
			float s = m_camera.orthographicSize;
			float n = m_camera.nearClipPlane;
			float f = m_camera.farClipPlane;
			float a = m_camera.aspect;
			float nx = s * (1.0f + p * (n-d));
			float fx = s * (1.0f + p * (f-d));
			var points = new [] {
				new Vector3( nx, nx/a, n),
				new Vector3(-nx, nx/a, n),
				new Vector3(-nx,-nx/a, n),
				new Vector3( nx,-nx/a, n),
				new Vector3( fx, fx/a, f),
				new Vector3(-fx, fx/a, f),
				new Vector3(-fx,-fx/a, f),
				new Vector3( fx,-fx/a, f),
				new Vector3(  s,  s/a, d),
				new Vector3( -s,  s/a, d),
				new Vector3( -s, -s/a, d),
				new Vector3(  s, -s/a, d),
			};
			var lines = new [] {
				0,1, 1,2, 2,3, 3,0,
				0,4, 1,5, 2,6, 3,7,
				4,5, 5,6, 6,7, 7,4,
				8,9, 9,10, 10,11, 11,8,
			};
			for (int i = 0; i < lines.Length; i += 2) {
				Gizmos.DrawLine(points[lines[i]], points[lines[i+1]]);
			}

			Gizmos.matrix = oldGizmosMatrix;
		}
	}
}