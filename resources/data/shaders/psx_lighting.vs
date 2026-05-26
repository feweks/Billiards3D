#version 330

in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec3 vertexNormal;
in vec4 vertexColor;

uniform mat4 mvp;
uniform mat4 matModel;
uniform int useLighting;

struct Light
{
    int enabled;
    vec3 position;
    vec3 color;
    float intensity;
};

#define MAX_LIGHTS 8
uniform Light lights[MAX_LIGHTS];
uniform vec3 ambientColor;

out vec2 fragTexCoord;
out vec4 fragColor;

void main()
{
    vec3 totalLighting = vec3(1.0);

    if (useLighting == 1)
    {
        vec3 worldPos = vec3(matModel * vec4(vertexPosition, 1.0));
        vec3 normal = normalize(mat3(matModel) * vertexNormal);

        totalLighting = ambientColor;

        for (int i = 0; i < MAX_LIGHTS; i++) 
        {
            if (lights[i].enabled == 1)
            {
                vec3 lightDir = lights[i].position - worldPos;
                float distance = length(lightDir);
                lightDir = normalize(lightDir);

                float NdotL = max(dot(normal, lightDir), 0.0);
                float NdotL2 = max(dot(-normal, lightDir), 0.0);
                NdotL = max(NdotL, NdotL2);

                float attenuation = lights[i].intensity / (1.0 + 0.1 * distance + 0.05 * distance * distance);

                totalLighting += lights[i].color * NdotL * attenuation;
            }
        }
    }

    fragColor = vec4(totalLighting, 1.0);
    fragTexCoord = vertexTexCoord;

    gl_Position = mvp * vec4(vertexPosition, 1.0);
}