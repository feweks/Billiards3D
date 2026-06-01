#version 330

in vec2 fragTexCoord;
in vec3 fragWorldPos;
in vec3 fragNormal;

uniform sampler2D texture0;
uniform vec4 colDiffuse;
uniform int useLighting;

struct Light
{
    int enabled;
    vec3 position;
    vec3 color;
    float intensity;
    
    vec3 direction;    
    float cutoff;      
    float spotExponent;
};

#define MAX_LIGHTS 16
uniform Light lights[MAX_LIGHTS];
uniform vec3 ambientColor;

out vec4 finalColor;

void main()
{
    vec4 texColor = texture(texture0, fragTexCoord);
    if (texColor.a < 0.1)
    {
        discard;
    }

    vec3 totalLighting = vec3(1.0);

    if (useLighting == 1)
    {
        vec3 normal = normalize(fragNormal);
        totalLighting = ambientColor;

        for (int i = 0; i < MAX_LIGHTS; i++) 
        {
            if (lights[i].enabled == 1)
            {
                vec3 lightDir = lights[i].position - fragWorldPos;
                float distance = length(lightDir);
                lightDir = normalize(lightDir);
                
                float NdotL = max(dot(normal, lightDir), 0.0);
                float attenuation = lights[i].intensity / (1.0 + 0.1 * distance + 0.05 * distance * distance);
                float dirLengthSq = dot(lights[i].direction, lights[i].direction);
                
                if (dirLengthSq < 0.0001)
                {
                    totalLighting += lights[i].color * NdotL * attenuation;
                }
                else
                {
                    float spotEffect = dot(normalize(lights[i].direction), -lightDir);
                    
                    if (spotEffect > lights[i].cutoff) 
                    {
                        spotEffect = pow(spotEffect, lights[i].spotExponent);
                        totalLighting += lights[i].color * NdotL * attenuation * spotEffect;
                    }
                }
            }
        }
    }

    finalColor = texture(texture0, fragTexCoord) * vec4(totalLighting, 1.0) * colDiffuse;
}