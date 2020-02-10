using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Commands;

// XEP-0107: User Mood

namespace YetAnotherXmppClient.Protocol.Handler
{
    public enum Mood
    {
        afraid, // Impressed with fear or apprehension; in fear; apprehensive.

        amazed, // Astonished; confounded with fear, surprise or wonder.

        amorous, // Inclined to love; having a propensity to love, or to sexual enjoyment; loving, fond, affectionate, passionate, lustful, sexual, etc.

        angry, // Displaying or feeling anger, i.e., a strong feeling of displeasure, hostility or antagonism towards someone or something, usually combined with an urge to harm.

        annoyed, // To be disturbed or irritated, especially by continued or repeated acts.

        anxious, // Full of anxiety or disquietude; greatly concerned or solicitous, esp. respecting something future or unknown; being in painful suspense.

        aroused, // To be stimulated in one's feelings, especially to be sexually stimulated.

        ashamed, // Feeling shame or guilt.

        bored, // Suffering from boredom; uninterested, without attention.

        brave, // Strong in the face of fear; courageous.

        calm, // Peaceful, quiet.

        cautious, // Taking care or caution; tentative.

        cold, // Feeling the sensation of coldness, especially to the point of discomfort.

        confident, // Feeling very sure of or positive about something, especially about one's own capabilities.

        confused, // Chaotic, jumbled or muddled.

        contemplative, // Feeling introspective or thoughtful.

        contented, // Pleased at the satisfaction of a want or desire; satisfied.

        cranky, // Grouchy, irritable; easily upset.

        crazy, // Feeling out of control; feeling overly excited or enthusiastic.

        creative, // Feeling original, expressive, or imaginative.

        curious, // Inquisitive; tending to ask questions, investigate, or explore.

        dejected, // Feeling sad and dispirited.

        depressed, // Severely despondent and unhappy.

        disappointed, // Defeated of expectation or hope; let down.

        disgusted, // Filled with disgust; irritated and out of patience.

        dismayed, // Feeling a sudden or complete loss of courage in the face of trouble or danger.

        distracted, // Having one's attention diverted; preoccupied.

        embarrassed, // Having a feeling of shameful discomfort.

        envious, // Feeling pain by the excellence or good fortune of another.

        excited, // Having great enthusiasm.

        flirtatious, // In the mood for flirting.

        frustrated, // Suffering from frustration; dissatisfied, agitated, or discontented because one is unable to perform an action or fulfill a desire.

        grateful, // Feeling appreciation or thanks.

        grieving, // Feeling very sad about something, especially something lost; mournful; sorrowful.

        grumpy, // Unhappy and irritable.

        guilty, // Feeling responsible for wrongdoing; feeling blameworthy.

        happy, // Experiencing the effect of favourable fortune; having the feeling arising from the consciousness of well-being or of enjoyment; enjoying good of any kind, as peace, tranquillity, comfort; contented; joyous.

        hopeful, // Having a positive feeling, belief, or expectation that something wished for can or will happen.

        hot, // Feeling the sensation of heat, especially to the point of discomfort.

        humbled, // Having or showing a modest or low estimate of one's own importance; feeling lowered in dignity or importance.

        humiliated, // Feeling deprived of dignity or self-respect.

        hungry, // Having a physical need for food.

        hurt, // Wounded, injured, or pained, whether physically or emotionally.

        impressed, // Favourably affected by something or someone.

        in_awe, // Feeling amazement at something or someone; or feeling a combination of fear and reverence.

        in_love, // Feeling strong affection, care, liking, or attraction..

        indignant, // Showing anger or indignation, especially at something unjust or wrong.

        interested, // Showing great attention to something or someone; having or showing interest.

        intoxicated, // Under the influence of alcohol; drunk.

        invincible, // Feeling as if one cannot be defeated, overcome or denied.

        jealous, // Fearful of being replaced in position or affection.

        lonely, // Feeling isolated, empty, or abandoned.

        lost, // Unable to find one's way, either physically or emotionally.

        lucky, // Feeling as if one will be favored by luck.

        mean, // Causing or intending to cause intentional harm; bearing ill will towards another; cruel; malicious.

        moody, // Given to sudden or frequent changes of mind or feeling; temperamental.

        nervous, // Easily agitated or alarmed; apprehensive or anxious.

        neutral, // Not having a strong mood or emotional state.

        offended, // Feeling emotionally hurt, displeased, or insulted.

        outraged, // Feeling resentful anger caused by an extremely violent or vicious attack, or by an offensive, immoral, or indecent act.

        playful, // Interested in play; fun, recreational, unserious, lighthearted; joking, silly.

        proud, // Feeling a sense of one's own worth or accomplishment.

        relaxed, // Having an easy-going mood; not stressed; calm.

        relieved, // Feeling uplifted because of the removal of stress or discomfort.

        remorseful, // Feeling regret or sadness for doing something wrong.

        restless, // Without rest; unable to be still or quiet; uneasy; continually moving.

        sad, // Feeling sorrow; sorrowful, mournful.

        sarcastic, // Mocking and ironical.

        satisfied, // Pleased at the fulfillment of a need or desire.

        serious, // Without humor or expression of happiness; grave in manner or disposition; earnest; thoughtful; solemn.

        shocked, // Surprised, startled, confused, or taken aback.

        shy, // Feeling easily frightened or scared; timid; reserved or coy.

        sick, // Feeling in poor health; ill.

        sleepy, // Feeling the need for sleep.

        spontaneous, // Acting without planning; natural; impulsive.

        stressed, // Suffering emotional pressure.

        strong, // Capable of producing great physical force; or, emotionally forceful, able, determined, unyielding.

        surprised, // Experiencing a feeling caused by something unexpected.

        thankful, // Showing appreciation or gratitude.

        thirsty, // Feeling the need to drink.

        tired, // In need of rest or sleep.

        undefined, // [Feeling any emotion not defined here.]

        weak, // Lacking in force or ability, either physical or emotional.

        worried, // Thinking about unpleasant things that have happened or that might happen; feeling afraid and unhappy.
    }

    internal class UserMoodProtocolHandler : ProtocolHandlerBase, IAsyncCommandHandler<SetMoodCommand>
    {
        public UserMoodProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters, IMediator mediator)
            : base(xmppStream, runtimeParameters, mediator)
        {
            this.Mediator.RegisterHandler<SetMoodCommand>(this);
        }

        /// <param name="mood">if null, then mood is disabled</param>
        /// <param name="text"></param>
        /// <returns></returns>
        public Task SetMoodAsync(Mood? mood, string text = null)
        {
            var command = new PublishEventCommand
                              {
                                  Node = "http://jabber.org/protocol/mood",
                                  Content = new XElement(XNames.mood_mood, mood.ToXElement(), string.IsNullOrEmpty(text) ? null : new XElement("text", text))
                              };

            return this.Mediator.ExecuteAsync(command);
        }

        Task IAsyncCommandHandler<SetMoodCommand>.HandleCommandAsync(SetMoodCommand command)
        {
            return this.SetMoodAsync(command.Mood, command.Text);
        }
    }

    internal static class MoodExtensions
    {
        public static XElement ToXElement(this Mood? mood)
        {
            return mood.HasValue ? new XElement(mood.Value.ToString()) : null;
        }
    }
}
