#version 330

in vec2 fragTexCoord;
out vec4 finalColor;

uniform sampler2D texture0;

vec3 posterize(vec3 color, float steps)
{
    return floor(color * steps) / steps;
}

void main()
{
    vec4 texelColor = texture(texture0, fragTexCoord);
    
    texelColor.rgb = posterize(texelColor.rgb, 20.0);
    
    finalColor = texelColor;
}