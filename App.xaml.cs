using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Plantify.Data;
using Plantify.Data.Repositories;
using Plantify.ViewModels;
using Plantify.Services;
using CommunityToolkit.Mvvm.Messaging;
using System.IO;
using System.Windows;
using System.Collections.Generic;
using System;
using Plantify.Models;
using System.Linq;

namespace Plantify
{
    public partial class App : Application
    {
        private static IHost? _host;

        public static IHost Host => _host ??= CreateHostBuilder(new string[] { }).Build();

        public static IServiceProvider Services => Host.Services;

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseSqlServer(context.Configuration.GetConnectionString("DefaultConnection"));
                    }, ServiceLifetime.Transient);

                    services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
                    services.AddScoped<IPlantRepository, PlantRepository>();
                    services.AddScoped<IUserPlantRepository, UserPlantRepository>();
                    services.AddScoped<INotificationRepository, NotificationRepository>();
                    services.AddScoped<IUnitOfWork, UnitOfWork>();

                    services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
                    services.AddSingleton(s => new AuthenticationService(
                        s.GetRequiredService<IUnitOfWork>(), 
                        s.GetRequiredService<IMessenger>()));
                    services.AddTransient<IDialogService, DialogService>();
                    
                    services.AddSingleton<MainViewModel>();
                    services.AddTransient<LoginViewModel>();
                    services.AddTransient<RegisterViewModel>();
                    services.AddTransient<DashboardViewModel>();
                    services.AddTransient<EncyclopediaViewModel>();
                    services.AddTransient<PlantManagementViewModel>();
                    services.AddTransient<MyGardenViewModel>();
                    services.AddTransient<AddUserPlantViewModel>();
                    services.AddTransient<ScheduleViewModel>();
                    services.AddTransient<SettingsViewModel>();
                    services.AddTransient<AdminPanelViewModel>();
                    services.AddTransient<StatisticsViewModel>();
                    services.AddTransient<AddUserViewModel>();
                    services.AddTransient<PremiumPurchaseViewModel>();
                    services.AddTransient<PlantDetailViewModel>();
                    services.AddTransient<AddPlantSuggestionViewModel>();
                    services.AddTransient<AddEditPlantViewModel>();
                    services.AddTransient<InputDialogViewModel>();
                    services.AddSingleton(s => new NotificationViewModel(
                        s.GetRequiredService<IMessenger>(), 
                        s.GetRequiredService<IUnitOfWork>(),
                        s.GetRequiredService<AuthenticationService>()));

                    services.AddSingleton<MainWindow>();
                });

        protected override async void OnStartup(StartupEventArgs e)
        {
            await Host.StartAsync();

            using (var scope = Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<AppDbContext>();

                    if (!context.Roles.Any())
                    {
                        context.Roles.AddRange(
                            new Models.Role { Name = "Администратор" },
                            new Models.Role { Name = "Контент-менеджер" },
                            new Models.Role { Name = "Клиент" }
                        );
                        await context.SaveChangesAsync();
                    }

                    if (!context.Varieties.Any())
                    {
                        context.Varieties.AddRange(
                            new Variety { Name = "Суккуленты" },
                            new Variety { Name = "Лианы" },
                            new Variety { Name = "Лиственные" }
                        );
                        await context.SaveChangesAsync();
                    }

                    if (!context.LightRequirements.Any())
                    {
                        context.LightRequirements.AddRange(
                            new LightRequirement { Name = "Теневыносливые" },
                            new LightRequirement { Name = "Светолюбивые" }
                        );
                        await context.SaveChangesAsync();
                    }

                    if (!context.Plants.Any())
                    {
                        var varietySucculent = await context.Varieties.FirstAsync(v => v.Name == "Суккуленты");
                        var varietyLiana = await context.Varieties.FirstAsync(v => v.Name == "Лианы");
                        var varietyDeciduous = await context.Varieties.FirstAsync(v => v.Name == "Лиственные");

                        var lightShadeTolerant = await context.LightRequirements.FirstAsync(l => l.Name == "Теневыносливые");
                        var lightSunLoving = await context.LightRequirements.FirstAsync(l => l.Name == "Светолюбивые");

                        var snakePlant = new Models.Plant { Name = "Замиокулькас (Денежное дерево)", WateringInterval = 21, FertilizingInterval = 45, LightRequirement = lightShadeTolerant, Variety = varietySucculent, ImagePath = "Images/Plants/zamioculcas.png" };
                        var snakePlantSections = new[]
                        {
                            new Models.PlantSection { Title = "Происхождение и особенности растения", Content = "Денежное дерево, известное ботаникам как Crassula ovata, — это суккулент, родиной которого являются засушливые регионы Южной Африки . В естественной среде оно привыкло к суровым условиям: каменистой почве, яркому солнцу и редким дождям. Благодаря этому растение научилось запасать влагу в своих толстых, мясистых листьях, чтобы переживать долгие периоды засухи. Свое народное название «денежное» оно получило из-за округлой формы листьев, напоминающих монетки, и в более позднее время, с распространением учения фэн-шуй, стало считаться символом материального благополучия и удачи ." },
                            new Models.PlantSection { Title = "Освещение и полив", Content = "Привычка к африканскому солнцу сделала денежное дерево настоящим светолюбивым растением. Для сохранения компактной формы и насыщенного зеленого цвета листьев ему необходимо обеспечить минимум 4 часа прямого солнечного света в день . Лучшим местом для него станет подоконник южного или юго-восточного окна. При недостатке света его стебли начнут вытягиваться, бледнеть и слабеть . Что касается полива, то здесь главное правило — умеренность. Между поливами земляной ком должен полностью просохнуть . Летом растение поливают примерно раз в 2-3 недели, а с наступлением зимы полив сокращают до одного раза в месяц и реже. Самый верный способ проверить необходимость полива — потрогать почву пальцем: если она сухая на глубине 2-3 см, можно смело поливать ." },
                            new Models.PlantSection { Title = "Удобрение и уход", Content = "Денежное дерево по праву считается одним из самых неприхотливых питомцев, уход за которым под силу даже новичку. Оно не требует частых подкормок и повышенной влажности воздуха, прекрасно адаптируясь к условиям обычной квартиры. Удобрять растение нужно только в период активного роста, с весны до конца лета. Для этого идеально подойдет жидкое удобрение для кактусов и суккулентов, которое вносят примерно раз в 1-2 месяца . Осенью и зимой, когда рост замедляется, подкормки полностью прекращают. Единственное, что может потребоваться взрослому растению, — это пересадка раз в 2-3 года в свежий, хорошо дренированный грунт ." },
                            new Models.PlantSection { Title = "Болезни и возможные проблемы", Content = "Большинство проблем с денежным деревом возникает из-за неправильного ухода, и самая частая из них — корневая гниль, вызванная чрезмерным поливом . Ее симптомами являются желтеющие, мягкие и опадающие листья, а также размягченный стебель. Еще одно распространенное последствие избытка влаги, особенно в холодное время года, — «водянка» (эдема), которая проявляется в виде коричневых, похожих на пробку наростов на листьях . Прямые солнечные лучи после долгого нахождения в тени могут вызвать ожоги в виде сухих коричневых пятен. Из вредителей иногда встречаются мучнистые червецы, с которыми борются с помощью инсекцидного мыла ." },
                            new Models.PlantSection { Title = "Итоги", Content = "Итак, денежное дерево — это классический представитель суккулентов, уход за которым строится на трех «китах»: обилие солнца, скудный полив и редкие подкормки. Оно способно накапливать воду в листьях, чтобы переживать засуху, и крайне не любит переувлажнения . При соблюдении этих простых правил это выносливое растение будет радовать вас своим декоративным видом долгие годы, не доставляя особых хлопот." }
                        };
                        foreach(var s in snakePlantSections) snakePlant.Sections.Add(s);


                        var monstera = new Models.Plant { Name = "Монстера Деликатесная", WateringInterval = 10, FertilizingInterval = 21, LightRequirement = lightShadeTolerant, Variety = varietyLiana, ImagePath = "Images/Plants/monstera.png" };
                        var monsteraSections = new[]
                        {
                            new Models.PlantSection { Title = "Происхождение и особенности", Content = "Монстера деликатесная (Monstera deliciosa), она же «швейцарский сыр» или «адамово ребро», — это тропическая лиана, родиной которой являются влажные леса от юга Мексики до Панамы. В дикой природе она использует свои воздушные корни, чтобы взбираться по стволам деревьев поближе к свету, пробивающемуся сквозь густую листву, и может достигать 10 метров и более в высоту. Ее знаменитые резные листья с отверстиями появляются не сразу: молодые растения имеют цельные сердцевидные листья, а характерные прорези и «окошечки» (этот процесс называется фенестрацией) формируются по мере взросления, помогая растению выдерживать тропические ливни и пропускать свет к нижним листьям. Латинское название 'monstera' означает «монстр» или «необычная» и дано растению именно за его удивительные листья." },
                            new Models.PlantSection { Title = "Освещение и полив", Content = "Привычка расти под пологом тропического леса, в кружевной тени, многое говорит о потребностях монстеры в освещении. Ей жизненно необходим яркий, но при этом исключительно рассеянный свет. Прямые солнечные лучи почти гарантированно вызовут ожоги на листьях — сухие коричневые или желтые пятна. Если же света будет недостаточно, новые листья вырастут мелкими и без характерных отверстий, а побеги станут вытянутыми и тонкими. Что касается полива, монстера любит влагу, но не терпит застоя воды у корней. Поливать ее нужно обильно, но только тогда, когда верхний слой грунта просохнет примерно на 2-3 сантиметра в глубину. Зимой, когда рост замедляется, полив существенно сокращают, дожидаясь, пока просохнет уже треть горшка, но не давая листьям завянуть." },
                            new Models.PlantSection { Title = "Влажность и удобрение", Content = "Будучи уроженкой тропиков, монстера очень ценит высокую влажность воздуха. В обычной квартире, особенно с центральным отоплением, сухой воздух может стать для нее проблемой, вызывая появление коричневых и сухих кончиков у листьев. Регулярное опрыскивание листьев отстоянной водой, их протирание влажной губкой от пыли или использование увлажнителя воздуха помогут создать для нее комфортные условия. В период активного роста, с весны до конца лета, монстеру полезно подкармливать. Для этого идеально подойдет жидкое удобрение для декоративно-лиственных растений, которое вносят примерно раз в 2-4 недели. Осенью и зимой подкормки следует прекратить, чтобы дать растению отдохнуть." },
                            new Models.PlantSection { Title = "Уход и возможные проблемы", Content = "В целом уход за монстерой не считается сложным, но у него есть свои особенности. Это лиана, поэтому для здорового и красивого роста ей, как правило, требуется опора — например, моховой шест или кокосовая палочка, за которую она сможет цепляться воздушными корнями. Эти воздушные корни не нужно обрезать: их можно направлять в горшок с землей или в отдельные емкости с водой, чтобы растение получало дополнительное питание и влагу. Что касается болезней, то монстера достаточно устойчива к ним, и большинство проблем возникает из-за ошибок в уходе. Самая частая неприятность — это корневая гниль, вызванная чрезмерным поливом и тяжелым грунтом. Ее симптомами являются пожелтение и увядание листьев, а также почернение стеблей. Из вредителей иногда можно встретить паутинного клеща или мучнистого червеца, особенно если воздух слишком сухой." },
                            new Models.PlantSection { Title = "Важный нюанс и итоги", Content = "При работе с монстерой стоит помнить, что ее сок содержит оксалат кальция — кристаллы, которые могут вызвать раздражение кожи и слизистых оболочек. Поэтому после обрезки листьев или стеблей лучше мыть руки, а само растение размещать в местах, недоступных для маленьких детей и домашних животных. Подводя итог, можно сказать, что монстера — это относительно неприхотливая и очень благодарная лиана, которая идеально впишется в интерьер. Уход за ней строится на четырех главных правилах: много яркого, но рассеянного света, аккуратный полив без застоя воды, высокую влажность воздуха и регулярные подкормки в теплое время года. При их соблюдении она будет радовать вас своей экзотической красотой долгие годы." }
                        };
                        foreach(var s in monsteraSections) monstera.Sections.Add(s);

                        var ficus = new Models.Plant { Name = "Фикус Бенджамина", WateringInterval = 7, FertilizingInterval = 14, LightRequirement = lightSunLoving, Variety = varietyDeciduous, ImagePath = "Images/Plants/ficus.png" };
                        var ficusSections = new[]
                        {
                            new Models.PlantSection { Title = "Происхождение и особенности", Content = "Фикус — это обширный род растений из семейства Тутовые, объединяющий более 800 видов, и его «родственником» является инжир . Большинство комнатных фикусов — вечнозеленые деревья, родиной которых являются тропические леса Индии, Китая, Юго-Восточной Азии и Африки. В природе многие из них начинают жизнь как эпифиты, оплетая воздушными корнями стволы других деревьев. В комнатной культуре самые популярные виды — это каучуконосный фикус с крупными темными листьями, изящный фикус Бенджамина с мелкими листочками и эффектный лировидный фикус, чьи листья напоминают музыкальный инструмент . Именно за свою разнообразную красоту и относительную неприхотливость фикусы уже много десятилетий остаются классикой домашнего озеленения." },
                            new Models.PlantSection { Title = "Освещение и полив", Content = "Фикус — растение довольно светолюбивое, но при этом не переносит прямых солнечных лучей. Для него идеально подойдет хорошо освещенное место с ярким, но рассеянным светом, например, рядом с восточным или западным окном . Исключение составляют пестролистные сорта, которым для сохранения яркой окраски требуется больше света . Что касается полива, то здесь фикус проявляет характер: он любит умеренно влажную почву летом, когда его поливают примерно 1-2 раза в неделю, но крайне не терпит застоя воды у корней . На избыточный полив он часто реагирует сбрасыванием листьев. Зимой, в период покоя, полив значительно сокращают, примерно до одного раза в 10-12 дней, дожидаясь, пока верхний слой грунта хорошо просохнет . Воду из поддона после полива всегда нужно сливать." },
                            new Models.PlantSection { Title = "Влажность и удобрение", Content = "Будучи тропическим растением, фикус очень ценит высокую влажность воздуха, что особенно актуально во время отопительного сезона . Он с благодарностью отзовется на регулярное опрыскивание листьев отстоянной водой, теплый душ и простое протирание листьев влажной губкой, что также помогает убрать пыль и позволяет растению лучше «дышать» . Для поддержания сил фикусу необходимы регулярные подкормки в период активного роста, с весны до осени. Идеально подойдут жидкие комплексные удобрения для декоративно-лиственных растений, которые вносят примерно раз в две недели . Зимой, когда рост замедляется, подкормки следует прекратить или свести к минимуму, удобряя не чаще одного раза в месяц-полтора ." },
                            new Models.PlantSection { Title = "Уход и возможные проблемы", Content = "В целом уход за фикусом нельзя назвать сложным, однако у него есть одна ярко выраженная особенность — он большой консерватор и очень не любит перемен . Растение может сбросить листья в ответ на простой перенос горшка на новое место, на сквозняк, резкий перепад температуры или поворот относительно источника света . Поэтому, выбрав для него подходящее место, постарайтесь не переставлять его без крайней необходимости. Самая частая проблема со здоровьем фикуса — это корневая гниль, возникающая из-за чрезмерного полива и сигнализирующая о себе пожелтением и опаданием листьев . Из вредителей на фикус могут напасть щитовка, мучнистый червец и паутинный клещ, который особенно любит сухой воздух ." },
                            new Models.PlantSection { Title = "Важный нюанс и итоги", Content = "Стоит помнить, что млечный сок фикуса, особенно каучуконосного, может вызвать раздражение при попадании на кожу, поэтому все работы по обрезке лучше проводить в перчатках, а само растение размещать подальше от детей и домашних животных. Подводя итог, фикус — это благодарное и долговечное растение, если обеспечить ему несколько базовых условий: постоянное место с ярким, но рассеянным светом, осторожный полив без застоя воды, высокую влажность воздуха и регулярное питание в теплое время года. При соблюдении этих несложных правил он будет радовать вас своей мощной зеленью долгие годы." }
                        };
                        foreach(var s in ficusSections) ficus.Sections.Add(s);
                        
                        context.Plants.AddRange(snakePlant, monstera, ficus);
                        await context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while seeding the database: {ex.Message}");
                }
            }

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            using (Host)
            {
                await Host.StopAsync();
            }

            base.OnExit(e);
        }
    }
}
