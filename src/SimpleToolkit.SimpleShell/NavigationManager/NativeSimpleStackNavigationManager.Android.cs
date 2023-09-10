﻿#if ANDROID

using Android.Views.Animations;
using AndroidX.Fragment.App;
using Microsoft.Maui.Platform;
using SimpleToolkit.SimpleShell.Platform;
using AView = Android.Views.View;

namespace SimpleToolkit.SimpleShell.NavigationManager;

public partial class NativeSimpleStackNavigationManager
{
    protected async Task NavigateNativelyToPageInContainer(
        SimpleShell shell,
        IView previousShellItemContainer,
        IView previousShellSectionContainer,
        IView previousPage,
        bool isPreviousPageRoot,
        bool animated = true)
    {
        var newPageView = GetPlatformView(currentPage);
        var oldPageView = GetPlatformView(previousPage);
        var newSectionContainer = GetPlatformView(currentShellSectionContainer);
        var oldSectionContainer = GetPlatformView(previousShellSectionContainer);
        var newItemContainer = GetPlatformView(currentShellItemContainer);
        var oldItemContainer = GetPlatformView(previousShellItemContainer);

        var to = GetFirstDifferent(newItemContainer, newSectionContainer, newPageView, oldItemContainer, oldSectionContainer);
        var from = GetFirstDifferent(oldItemContainer, oldSectionContainer, oldPageView, newItemContainer, newSectionContainer);

        to?.Animation?.Cancel();
        to?.ClearAnimation();
        from?.Animation?.Cancel();
        from?.ClearAnimation();

        if (oldPageView?.Parent is Android.Views.ViewGroup pageVg)
            pageVg.ClearDisappearingChildren();
        if (oldSectionContainer?.Parent is Android.Views.ViewGroup sectionVg)
            sectionVg.ClearDisappearingChildren();
        if (oldItemContainer?.Parent is Android.Views.ViewGroup itemVg)
            itemVg.ClearDisappearingChildren();

        AddPlatformPageToContainer(currentPage, shell, false, isCurrentPageRoot: isCurrentPageRoot);

        if (from is not null)
        {
            var enterAnimation = AnimationUtils.LoadAnimation(mauiContext.Context, Resource.Animation.simpleshell_fade_in);
            var leaveAnimation = AnimationUtils.LoadAnimation(mauiContext.Context, Resource.Animation.simpleshell_fade_out);
            var noneAnimation = AnimationUtils.LoadAnimation(mauiContext.Context, Resource.Animation.simpleshell_none);

            noneAnimation.Duration = leaveAnimation.Duration;

            to.Visibility = Android.Views.ViewStates.Invisible;

            // The delay is needed to play the animation, but ideally it should not be
            await Task.Delay(10).ConfigureAwait(true);

            to.StartAnimation(enterAnimation);
            from.StartAnimation(leaveAnimation);
            to.Visibility = Android.Views.ViewStates.Visible;
            from.Visibility = Android.Views.ViewStates.Visible;

            if (from != oldPageView)
                oldPageView?.StartAnimation(noneAnimation);
            if (from != oldSectionContainer)
                oldSectionContainer?.StartAnimation(noneAnimation);
            if (from != oldItemContainer)
                oldItemContainer?.StartAnimation(noneAnimation);

            // Animation is played even if the view is removed from its container in the meantime
            // The view is moved to a disappearing children collection of the container
            if (previousPage != currentPage)
                RemovePlatformPageFromContainer(previousPage, previousShellItemContainer, previousShellSectionContainer, isCurrentPageRoot, isPreviousPageRoot);
        }
    }

    protected void HandleNewStack(IReadOnlyList<IView> newPageStack, bool animated = true)
    {
        var isRootNavigation = newPageStack.Count == 1 && NavigationStack.Count == 1;
        var switchFragments = (NavigationStack.Count == 0) ||
            (!isRootNavigation && newPageStack[newPageStack.Count - 1] != NavigationStack[NavigationStack.Count - 1]);
        var oldPageStack = NavigationStack;
        NavigationStack = newPageStack;

        if (!switchFragments)
            return;

        var platformView = isCurrentPageRoot ?
            navigationFrame :
            GetPlatformView(newPageStack[newPageStack.Count - 1]);
        var fragment = CreateFragment(platformView);

        var fragmentManager = mauiContext.Context.GetFragmentManager();
        var transaction = fragmentManager.BeginTransaction();

        transaction.SetReorderingAllowed(true);
        if (animated)
        {
            var shouldPop = ShouldPop(newPageStack, oldPageStack);

            transaction.SetCustomAnimations(
                shouldPop ? Resource.Animation.simpleshell_none : Resource.Animation.simpleshell_enter_right,
                shouldPop ? Resource.Animation.simpleshell_exit_right : Resource.Animation.simpleshell_none);
        }
        transaction.Replace(rootContainer.Id, fragment);
        transaction.Commit();
    }

    private static Fragment CreateFragment(AView view)
    {
        if (view.Parent is Android.Views.ViewGroup vg)
            vg.RemoveView(view);

        return new SimpleFragment(view);
    }

    private static bool ShouldPop(IReadOnlyList<IView> newPageStack, IReadOnlyList<IView> oldPageStack)
    {
        IView lastSame = null;

        for (int i = 0; i < newPageStack.Count; i++)
        {
            if (i < oldPageStack.Count && newPageStack[i] == oldPageStack[i])
                lastSame = newPageStack[i];
            else
                break;
        }

        return (lastSame is null && oldPageStack.Count > 0 && newPageStack[0] != oldPageStack[0])
            || lastSame == newPageStack[newPageStack.Count - 1];
    }
}

#endif